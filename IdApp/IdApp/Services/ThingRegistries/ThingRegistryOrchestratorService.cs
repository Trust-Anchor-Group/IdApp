using System;
using System.Threading.Tasks;
using IdApp.Pages.Things.ViewClaimThing;
using IdApp.Pages.Things.ViewThing;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Runtime.Inventory;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;

namespace IdApp.Services.ThingRegistries
{
	[Singleton]
	internal class ThingRegistryOrchestratorService : LoadableService, IThingRegistryOrchestratorService
	{
		private readonly ITagProfile tagProfile;
		private readonly IUiSerializer uiDispatcher;
		private readonly INeuronService neuronService;
		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private readonly INetworkService networkService;

		public ThingRegistryOrchestratorService(
			ITagProfile tagProfile,
			IUiSerializer uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			ILogService logService,
			INetworkService networkService)
		{
			this.tagProfile = tagProfile;
			this.uiDispatcher = uiDispatcher;
			this.neuronService = neuronService;
			this.navigationService = navigationService;
			this.logService = logService;
			this.networkService = networkService;
		}

		public override Task Load(bool isResuming)
		{
			if (this.BeginLoad())
				this.EndLoad(true);

			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
				this.EndUnload();

			return Task.CompletedTask;
		}

		#region Event Handlers

		#endregion

		public Task OpenClaimDevice(string Uri)
		{
			this.uiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				await this.navigationService.GoToAsync(nameof(ViewClaimThingPage), new ViewClaimThingNavigationArgs(Uri));
			});

			return Task.CompletedTask;
		}

		public async Task OpenSearchDevices(string Uri)
		{
			try
			{
				(SearchResultThing[] Things, string RegistryJid) = await this.neuronService.ThingRegistry.SearchAll(Uri);

				// TODO: Extract JID, NodeID, SourceID & Partition from URI, if available. If contains these properties, the result should
				// not be a search, but a view thing direct.

				switch (Things.Length)
				{
					case 0:
						await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.NoThingsFound);
						break;

					case 1:
						SearchResultThing Thing = Things[0];

						this.uiDispatcher.BeginInvokeOnMainThread(async () =>
						{
							Property[] Properties = ViewClaimThingViewModel.ToProperties(Thing.Tags);

							await this.navigationService.GoToAsync(nameof(ViewThingPage), new ViewThingNavigationArgs(new ContactInfo()
							{
								AllowSubscriptionFrom = false,
								BareJid = Thing.Jid,
								IsThing = true,
								LegalId = string.Empty,
								LegalIdentity = null,
								FriendlyName = ViewClaimThingViewModel.GetFriendlyName(Properties),
								MetaData = Properties,
								SourceId = Thing.Node.SourceId,
								Partition = Thing.Node.Partition,
								NodeId = Thing.Node.NodeId,
								Owner = false,
								RegistryJid = RegistryJid,
								SubcribeTo = null
							}));
						});
						break;

					default:
						// TODO: Search result list view
						break;
				}
			}
			catch (Exception ex)
			{
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}

	}
}