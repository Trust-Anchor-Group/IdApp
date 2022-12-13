using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdApp.Pages.Things.MyThings;
using IdApp.Pages.Things.ViewClaimThing;
using IdApp.Pages.Things.ViewThing;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.ThingRegistries
{
	[Singleton]
	internal class ThingRegistryOrchestratorService : LoadableService, IThingRegistryOrchestratorService
	{
		public ThingRegistryOrchestratorService()
		{
		}

		public override Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			if (this.BeginLoad(cancellationToken))
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
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				await this.NavigationService.GoToAsync(nameof(ViewClaimThingPage), new ViewClaimThingNavigationArgs(Uri));
			});

			return Task.CompletedTask;
		}

		public async Task OpenSearchDevices(string Uri)
		{
			try
			{
				(SearchResultThing[] Things, string RegistryJid) = await this.XmppService.SearchAll(Uri);

				switch (Things.Length)
				{
					case 0:
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NoThingsFound"]);
						break;

					case 1:
						SearchResultThing Thing = Things[0];

						this.UiSerializer.BeginInvokeOnMainThread(async () =>
						{
							Property[] Properties = ViewClaimThingViewModel.ToProperties(Thing.Tags);

							ContactInfo ContactInfo = await ContactInfo.FindByBareJid(Thing.Jid, Thing.Node.SourceId, Thing.Node.Partition, Thing.Node.NodeId);
							ContactInfo ??= new ContactInfo()
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

							await this.NavigationService.GoToAsync(nameof(ViewThingPage), new ViewThingNavigationArgs(ContactInfo,
								MyThingsViewModel.GetNotificationEvents(this, ContactInfo)));
						});
						break;

					default:
						throw new NotImplementedException("Multiple devices were returned. Feature not implemented.");
						// TODO: Search result list view
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		public async Task OpenDeviceReference(string Uri)
		{
			try
			{
				if (!this.XmppService.TryDecodeIoTDiscoDirectURI(Uri, out string Jid, out string SourceId,
					out string NodeId, out string PartitionId, out MetaDataTag[] Tags))
				{
					throw new InvalidOperationException("Not a direct reference URI.");
				}

				Property[] Properties = ViewClaimThingViewModel.ToProperties(Tags);
				ContactInfo Info = await ContactInfo.FindByBareJid(Jid, SourceId ?? string.Empty,
					PartitionId ?? string.Empty, NodeId ?? string.Empty);

				if (Info is null)
				{
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
						RegistryJid = this.XmppService.RegistryServiceJid,
						SubcribeTo = null
					};

					foreach (MetaDataTag Tag in Tags)
					{
						if (Tag.Name.ToUpper() == "R")
							Info.RegistryJid = Tag.StringValue;
					}
				}
				else if (!(Info.IsThing ?? false))
				{
					Info.MetaData = Merge(Info.MetaData, Properties);
					Info.IsThing = true;

					await Database.Update(Info);
				}

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(ViewThingPage), new ViewThingNavigationArgs(Info,
						MyThingsViewModel.GetNotificationEvents(this, Info)));
				});
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private static Property[] Merge(Property[] Stored, Property[] FromUri)
		{
			Dictionary<string, Property> Merged = new();
			bool Changed = false;

			if (Stored is not null)
			{
				foreach (Property P in Stored)
					Merged[P.Name] = P;
			}

			if (FromUri is not null)
			{
				foreach (Property P in FromUri)
				{
					if (!Merged.TryGetValue(P.Name, out Property P2) || P2.Value != P.Value)
					{
						Merged[P.Name] = P;
						Changed = true;
					}
				}
			}

			if (!Changed)
				return Stored;

			Property[] Result = new Property[Merged.Count];
			Merged.Values.CopyTo(Result, 0);

			return Result;
		}

	}
}
