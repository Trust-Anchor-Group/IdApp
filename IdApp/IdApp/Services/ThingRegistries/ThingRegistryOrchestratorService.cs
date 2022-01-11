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
		private readonly IUiSerializer uiSerializer;
		private readonly INeuronService neuronService;
		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private readonly INetworkService networkService;

		public ThingRegistryOrchestratorService(
			ITagProfile tagProfile,
			IUiSerializer uiSerializer,
			INeuronService neuronService,
			INavigationService navigationService,
			ILogService logService,
			INetworkService networkService)
		{
			this.tagProfile = tagProfile;
			this.uiSerializer = uiSerializer;
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
			this.uiSerializer.BeginInvokeOnMainThread(async () =>
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

				switch (Things.Length)
				{
					case 0:
						await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.NoThingsFound);
						break;

					case 1:
						SearchResultThing Thing = Things[0];

						this.uiSerializer.BeginInvokeOnMainThread(async () =>
						{
							Property[] Properties = ViewClaimThingViewModel.ToProperties(Thing.Tags);

							ContactInfo ContactInfo = await ContactInfo.FindByBareJid(Thing.Jid, Thing.Node.SourceId, Thing.Node.Partition, Thing.Node.NodeId);
							if (ContactInfo is null)
							{
								ContactInfo = new ContactInfo()
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
								};
							}

							await this.navigationService.GoToAsync(nameof(ViewThingPage), new ViewThingNavigationArgs(ContactInfo));
						});
						break;

					default:
						throw new NotImplementedException("Multiple devices were returned. Feature not implemented.");
						// TODO: Search result list view
				}
			}
			catch (Exception ex)
			{
				await this.uiSerializer.DisplayAlert(ex);
			}
		}

		public async Task OpenDeviceReference(string Uri)
		{
			try
			{
				if (!this.neuronService.ThingRegistry.TryDecodeIoTDiscoDirectURI(Uri, out string Jid, out string SourceId,
					out string NodeId, out string PartitionId, out MetaDataTag[] Tags))
				{
					throw new InvalidOperationException("Not a direct reference URI.");
				}

				ContactInfo Info = await ContactInfo.FindByBareJid(Jid, SourceId, PartitionId, NodeId);

				if (Info is null)
				{
					Property[] Properties = ViewClaimThingViewModel.ToProperties(Tags);

					Info = new ContactInfo()
					{
						AllowSubscriptionFrom = false,
						BareJid = Jid,
						IsThing = true,
						LegalId = string.Empty,
						LegalIdentity = null,
						FriendlyName = ViewClaimThingViewModel.GetFriendlyName(Properties) ?? Jid,
						MetaData = Properties,
						SourceId = SourceId,
						Partition = PartitionId,
						NodeId = NodeId,
						Owner = false,
						RegistryJid = string.Empty,
						SubcribeTo = null
					};
				}

				this.uiSerializer.BeginInvokeOnMainThread(async () =>
					await this.navigationService.GoToAsync(nameof(ViewThingPage), new ViewThingNavigationArgs(Info)));
			}
			catch (Exception ex)
			{
				await this.uiSerializer.DisplayAlert(ex);
			}
		}

	}
}