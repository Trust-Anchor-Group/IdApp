using IdApp.Extensions;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Resx;
using IdApp.Services.Notification.Contracts;
using IdApp.Services.Notification.Identities;
using IdApp.Services.Xmpp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Contracts
{
	[Singleton]
	internal class ContractOrchestratorService : LoadableService, IContractOrchestratorService
	{
		public ContractOrchestratorService()
		{
		}

		public override Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			if (this.BeginLoad(cancellationToken))
			{
				this.XmppService.ConnectionStateChanged += this.Contracts_ConnectionStateChanged;
				this.XmppService.Contracts.PetitionForPeerReviewIdReceived += this.Contracts_PetitionForPeerReviewIdReceived;
				this.XmppService.Contracts.PetitionForIdentityReceived += this.Contracts_PetitionForIdentityReceived;
				this.XmppService.Contracts.PetitionForSignatureReceived += this.Contracts_PetitionForSignatureReceived;
				this.XmppService.Contracts.PetitionedContractResponseReceived += this.Contracts_PetitionedSmartContractResponseReceived;
				this.XmppService.Contracts.PetitionForContractReceived += this.Contracts_PetitionForSmartContractReceived;
				this.XmppService.Contracts.PetitionedIdentityResponseReceived += this.Contracts_PetitionedIdentityResponseReceived;
				this.XmppService.Contracts.PetitionedPeerReviewIdResponseReceived += this.Contracts_PetitionedPeerReviewResponseReceived;
				this.XmppService.Contracts.SignaturePetitionResponseReceived += this.Contracts_SignaturePetitionResponseReceived;
				this.XmppService.Contracts.ContractProposalReceived += this.Contracts_ContractProposalReceived;
				this.XmppService.Contracts.ContractUpdated += this.Contracts_ContractUpdated;
				this.XmppService.Contracts.ContractSigned += this.Contracts_ContractSigned;

				this.EndLoad(true);
			}

			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.XmppService.ConnectionStateChanged -= this.Contracts_ConnectionStateChanged;
				this.XmppService.Contracts.PetitionForPeerReviewIdReceived -= this.Contracts_PetitionForPeerReviewIdReceived;
				this.XmppService.Contracts.PetitionForIdentityReceived -= this.Contracts_PetitionForIdentityReceived;
				this.XmppService.Contracts.PetitionForSignatureReceived -= this.Contracts_PetitionForSignatureReceived;
				this.XmppService.Contracts.PetitionedContractResponseReceived -= this.Contracts_PetitionedSmartContractResponseReceived;
				this.XmppService.Contracts.PetitionForContractReceived -= this.Contracts_PetitionForSmartContractReceived;
				this.XmppService.Contracts.PetitionedIdentityResponseReceived -= this.Contracts_PetitionedIdentityResponseReceived;
				this.XmppService.Contracts.PetitionedPeerReviewIdResponseReceived -= this.Contracts_PetitionedPeerReviewResponseReceived;
				this.XmppService.Contracts.SignaturePetitionResponseReceived -= this.Contracts_SignaturePetitionResponseReceived;
				this.XmppService.Contracts.ContractProposalReceived -= this.Contracts_ContractProposalReceived;
				this.XmppService.Contracts.ContractUpdated -= this.Contracts_ContractUpdated;
				this.XmppService.Contracts.ContractSigned -= this.Contracts_ContractSigned;

				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private async void Contracts_PetitionForPeerReviewIdReceived(object Sender, SignaturePetitionEventArgs e)
		{
			try
			{
				await this.NotificationService.NewEvent(new PeerRequestIdentityReviewNotificationEvent(e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_PetitionForIdentityReceived(object Sender, LegalIdentityPetitionEventArgs e)
		{
			try
			{
				LegalIdentity Identity;

				if (e.RequestedIdentityId == this.TagProfile.LegalIdentity?.Id)
					Identity = this.TagProfile.LegalIdentity;
				else
				{
					(bool Succeeded, LegalIdentity LegalId) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetLegalIdentity(e.RequestedIdentityId));
					if (Succeeded && !(LegalId is null))
						Identity = LegalId;
					else
						return;
				}

				if (Identity is null)
				{
					this.LogService.LogWarning(this.GetType().Name + "." + nameof(Contracts_PetitionForIdentityReceived) + "() - identity is missing or cannot be retrieved, ignore.");
					return;
				}

				if (Identity.State == IdentityState.Compromised ||
					Identity.State == IdentityState.Rejected)
				{
					await this.NetworkService.TryRequest(() =>
					{
						return this.XmppService.Contracts.SendPetitionIdentityResponse(
							e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
					});
				}
				else
					await this.NotificationService.NewEvent(new RequestIdentityNotificationEvent(e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_PetitionForSignatureReceived(object Sender, SignaturePetitionEventArgs e)
		{
			try
			{
				LegalIdentity Identity;

				if (e.SignatoryIdentityId == this.TagProfile.LegalIdentity?.Id)
					Identity = this.TagProfile.LegalIdentity;
				else
				{
					(bool Succeeded, LegalIdentity LegalId) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetLegalIdentity(e.SignatoryIdentityId));

					if (Succeeded && !(LegalId is null))
						Identity = LegalId;
					else
						return;
				}

				if (Identity is null)
				{
					this.LogService.LogWarning(this.GetType().Name + "." + nameof(Contracts_PetitionForSignatureReceived) + "() - identity is missing or cannot be retrieved, ignore.");
					return;
				}

				if (Identity.State == IdentityState.Compromised || Identity.State == IdentityState.Rejected)
				{
					await this.NetworkService.TryRequest(() =>
					{
						return this.XmppService.Contracts.SendPetitionSignatureResponse(e.SignatoryIdentityId, e.ContentToSign,
							new byte[0], e.PetitionId, e.RequestorFullJid, false);
					});
				}
				else
					await this.NotificationService.NewEvent(new RequestSignatureNotificationEvent(e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_PetitionedSmartContractResponseReceived(object Sender, ContractPetitionResponseEventArgs e)
		{
			try
			{
				await this.NotificationService.NewEvent(new ContractResponseNotificationEvent(e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_PetitionForSmartContractReceived(object Sender, ContractPetitionEventArgs e)
		{
			try
			{
				(bool Succeeded, Contract Contract) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetContract(e.RequestedContractId));

				if (!Succeeded)
					return;

				if (Contract.State == ContractState.Deleted || Contract.State == ContractState.Rejected)
					await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionContractResponse(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false));
				else
					await this.NotificationService.NewEvent(new ContractPetitionNotificationEvent(Contract, e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_PetitionedIdentityResponseReceived(object Sender, LegalIdentityPetitionResponseEventArgs e)
		{
			try
			{
				await this.NotificationService.NewEvent(new IdentityResponseNotificationEvent(e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_PetitionedPeerReviewResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				await this.NotificationService.NewEvent(new PeerIdentityReviewResponseNotificationEvent(e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_ConnectionStateChanged(object Sender, ConnectionStateChangedEventArgs e)
		{
			try
			{
				if (this.XmppService.IsOnline && this.XmppService.IsOnline)
				{
					if (!(this.TagProfile.LegalIdentity is null) && this.TagProfile.IsCompleteOrWaitingForValidation())
					{
						string id = this.TagProfile.LegalIdentity.Id;
						await Task.Delay(Constants.Timeouts.XmppInit);
						this.DownloadLegalIdentityInternal(id);
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_ContractProposalReceived(object Sender, ContractProposalEventArgs e)
		{
			Contract Contract;
			bool Succeeded;

			(Succeeded, Contract) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetContract(e.ContractId));
			if (!Succeeded || Contract is null)
				return;     // Contract not available.

			if (Contract.State != ContractState.Approved && Contract.State != ContractState.BeingSigned)
				return;     // Not in a state to be signed.

			ContractProposalNotificationEvent Event = new(e);
			Event.SetContract(Contract);

			await this.NotificationService.NewEvent(Event);
		}

		#endregion

		protected virtual async void DownloadLegalIdentityInternal(string LegalId)
		{
			// Run asynchronously so we're not blocking startup UI.
			try
			{
				await this.DownloadLegalIdentity(LegalId);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		protected async Task DownloadLegalIdentity(string LegalId)
		{
			bool isConnected =
				!(this.XmppService is null) &&
				await this.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect) &&
				this.XmppService.IsOnline;

			if (!isConnected)
				return;

			(bool succeeded, LegalIdentity identity) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetLegalIdentity(LegalId), displayAlert: false);
			if (succeeded)
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					string userMessage = null;
					bool gotoRegistrationPage = false;

					if (identity.State == IdentityState.Compromised)
					{
						userMessage = AppResources.YourLegalIdentityHasBeenCompromised;
						this.TagProfile.CompromiseLegalIdentity(identity);
						gotoRegistrationPage = true;
					}
					else if (identity.State == IdentityState.Obsoleted)
					{
						userMessage = AppResources.YourLegalIdentityHasBeenObsoleted;
						this.TagProfile.RevokeLegalIdentity(identity);
						gotoRegistrationPage = true;
					}
					else if (identity.State == IdentityState.Approved && !await this.XmppService.Contracts.HasPrivateKey(identity.Id))
					{
						bool Response = await this.UiSerializer.DisplayAlert(AppResources.WarningTitle, AppResources.UnableToGetAccessToYourPrivateKeys,
							AppResources.Continue, AppResources.Repair);

						if (Response)
							this.TagProfile.SetLegalIdentity(identity);
						else
						{
							try
							{
								File.WriteAllText(Path.Combine(this.StorageService.DataFolder, "Start.txt"), DateTime.Now.AddHours(1).Ticks.ToString());
							}
							catch (Exception ex)
							{
								this.LogService.LogException(ex);
							}

							await App.Stop();
							return;
						}
					}
					else
						this.TagProfile.SetLegalIdentity(identity);

					if (gotoRegistrationPage)
					{
						await App.Current.SetRegistrationPageAsync();

						// After navigating to the registration page, show the user why this happened.
						if (!string.IsNullOrWhiteSpace(userMessage))
						{
							// Do a begin invoke here so the page animation has time to finish,
							// and the view model loads state et.c. before showing the alert.
							// This gives a better UX experience.
							this.UiSerializer.BeginInvokeOnMainThread(async () =>
							{
								await this.UiSerializer.DisplayAlert(AppResources.YourLegalIdentity, userMessage);
							});
						}
					}
				});
			}
		}

		public async Task OpenLegalIdentity(string LegalId, string Purpose)
		{
			try
			{
				LegalIdentity identity = await this.XmppService.Contracts.GetLegalIdentity(LegalId);
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(identity));
				});
			}
			catch (ForbiddenException)
			{
				// This happens if you try to view someone else's legal identity.
				// When this happens, try to send a petition to view it instead.
				// Normal operation. Should not be logged.

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.PetitionIdentity(LegalId, Guid.NewGuid().ToString(), Purpose));
					if (succeeded)
					{
						await this.UiSerializer.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToTheOwner);
					}
				});
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex);
			}
		}

		public async Task OpenContract(string ContractId, string Purpose, Dictionary<string, object> ParameterValues)
		{
			try
			{
				Contract contract = await this.XmppService.Contracts.GetContract(ContractId);

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					if (contract.CanActAsTemplate && contract.State == ContractState.Approved)
					{
						await this.SettingsService.SaveState(Constants.KeyPrefixes.ContractTemplatePrefix + contract.ContractId, DateTime.Now);
						await this.NavigationService.GoToAsync(nameof(NewContractPage), new NewContractNavigationArgs(contract, ParameterValues));
					}
					else
						await this.NavigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(contract, false));
				});
			}
			catch (ForbiddenException)
			{
				// This happens if you try to view someone else's contract.
				// When this happens, try to send a petition to view it instead.
				// Normal operation. Should not be logged.

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.PetitionContract(ContractId, Guid.NewGuid().ToString(), Purpose));
					if (succeeded)
					{
						await this.UiSerializer.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToTheContract);
					}
				});
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex);
			}
		}

		/// <summary>
		/// TAG Signature request scanned.
		/// </summary>
		/// <param name="Request">Request string.</param>
		public async Task TagSignature(string Request)
		{
			int i = Request.IndexOf(',');
			if (i < 0)
				throw new InvalidOperationException("Invalid TAG Signature URI.");

			string JID = Request.Substring(0, i);
			string Key = Request[(i + 1)..];

			LegalIdentity ID = this.TagProfile?.LegalIdentity;
			if (ID is null)
				throw new InvalidOperationException("No Legal ID selected.");

			if (ID.State != IdentityState.Approved)
				throw new InvalidOperationException("Legal ID not approved.");

			string IdRef = this.TagProfile?.LegalIdentity?.Id ?? string.Empty;

			StringBuilder Xml = new();

			Xml.Append("<ql xmlns='https://tagroot.io/schema/Signature' key='");
			Xml.Append(XML.Encode(Key));
			Xml.Append("' legalId='");
			Xml.Append(XML.Encode(IdRef));
			Xml.Append("'/>");

			if (!this.XmppService.IsOnline)
				throw new InvalidOperationException("App is not connected to the network.");

			await this.XmppService.Xmpp.IqSetAsync(JID, Xml.ToString());
		}

		private async void Contracts_SignaturePetitionResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				await this.NotificationService.NewEvent(new SignatureResponseNotificationEvent(e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_ContractSigned(object Sender, ContractReferenceEventArgs e)
		{
			try
			{
				Contract Contract = await this.XmppService.Contracts.GetContract(e.ContractId);
				ContractReference Ref = await Database.FindFirstDeleteRest<ContractReference>(new FilterFieldEqualTo("ContractId", e.ContractId));
				DateTime TP = DateTime.UtcNow;

				if (Ref is null)
				{
					Ref = new ContractReference()
					{
						ContractId = e.ContractId,
						Created = TP,
						Updated = TP
					};

					await Ref.SetContract(Contract, this);
					await Database.Insert(Ref);
				}
				else
				{
					Ref.Updated = TP;

					await Ref.SetContract(Contract, this);
					await Database.Update(Ref);
				}

				await this.NotificationService.NewEvent(new ContractSignedNotificationEvent(Contract, e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Contracts_ContractUpdated(object Sender, ContractReferenceEventArgs e)
		{
			try
			{
				Contract Contract = await this.XmppService.Contracts.GetContract(e.ContractId);
				ContractReference Ref = await Database.FindFirstDeleteRest<ContractReference>(new FilterFieldEqualTo("ContractId", e.ContractId));
				DateTime TP = DateTime.UtcNow;

				if (Ref is null)
				{
					Ref = new ContractReference()
					{
						ContractId = e.ContractId,
						Created = TP,
						Updated = TP
					};

					await Ref.SetContract(Contract, this);
					await Database.Insert(Ref);
				}
				else
				{
					Ref.Updated = TP;

					await Ref.SetContract(Contract, this);
					await Database.Update(Ref);
				}

				await this.NotificationService.NewEvent(new ContractUpdatedNotificationEvent(Contract, e));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

	}
}
