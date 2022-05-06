using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Contracts.PetitionContract;
using IdApp.Pages.Contracts.PetitionSignature;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Pages.Identity.PetitionIdentity;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Pages.Registration.Registration;
using IdApp.Extensions;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Runtime.Inventory;
using IdApp.Services.Xmpp;
using IdApp.Resx;
using System.Threading;

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
				this.XmppService.ConnectionStateChanged += Contracts_ConnectionStateChanged;
				this.XmppService.Contracts.PetitionForPeerReviewIdReceived += Contracts_PetitionForPeerReviewIdReceived;
				this.XmppService.Contracts.PetitionForIdentityReceived += Contracts_PetitionForIdentityReceived;
				this.XmppService.Contracts.PetitionForSignatureReceived += Contracts_PetitionForSignatureReceived;
				this.XmppService.Contracts.PetitionedContractResponseReceived += Contracts_PetitionedSmartContractResponseReceived;
				this.XmppService.Contracts.PetitionForContractReceived += Contracts_PetitionForSmartContractReceived;
				this.XmppService.Contracts.PetitionedIdentityResponseReceived += Contracts_PetitionedIdentityResponseReceived;
				this.XmppService.Contracts.PetitionedPeerReviewIdResponseReceived += Contracts_PetitionedPeerReviewResponseReceived;
				this.XmppService.Contracts.ContractProposalReceived += Contracts_ContractProposalReceived;

				this.EndLoad(true);
			}
			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.XmppService.ConnectionStateChanged -= Contracts_ConnectionStateChanged;
				this.XmppService.Contracts.PetitionForPeerReviewIdReceived -= Contracts_PetitionForPeerReviewIdReceived;
				this.XmppService.Contracts.PetitionForIdentityReceived -= Contracts_PetitionForIdentityReceived;
				this.XmppService.Contracts.PetitionForSignatureReceived -= Contracts_PetitionForSignatureReceived;
				this.XmppService.Contracts.PetitionedContractResponseReceived -= Contracts_PetitionedSmartContractResponseReceived;
				this.XmppService.Contracts.PetitionForContractReceived -= Contracts_PetitionForSmartContractReceived;
				this.XmppService.Contracts.PetitionedIdentityResponseReceived -= Contracts_PetitionedIdentityResponseReceived;
				this.XmppService.Contracts.PetitionedPeerReviewIdResponseReceived -= Contracts_PetitionedPeerReviewResponseReceived;
				this.XmppService.Contracts.ContractProposalReceived -= Contracts_ContractProposalReceived;

				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private void Contracts_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					if (this.TagProfile.IsCompleteOrWaitingForValidation())
					{
						await this.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(e.RequestorIdentity, e));
					}
				}
			);
		}

		private async void Contracts_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
		{
			LegalIdentity identity;

			if (e.RequestedIdentityId == this.TagProfile.LegalIdentity?.Id)
			{
				identity = this.TagProfile.LegalIdentity;
			}
			else
			{
				(bool succeeded, LegalIdentity li) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetLegalIdentity(e.RequestedIdentityId));
				if (succeeded && !(li is null))
					identity = li;
				else
					return;
			}

			if (identity is null)
			{
				this.LogService.LogWarning(GetType().Name + "." + nameof(Contracts_PetitionForIdentityReceived) + "() - identity is missing or cannot be retrieved, ignore.");
				return;
			}

			if (identity.State == IdentityState.Compromised ||
				identity.State == IdentityState.Rejected)
			{
				await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionIdentityResponse(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false));
			}
			else
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					if (this.TagProfile.IsCompleteOrWaitingForValidation())
					{
						await this.NavigationService.GoToAsync(nameof(PetitionIdentityPage), new PetitionIdentityNavigationArgs(e.RequestorIdentity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose));
					}
				});
			}
		}

		private async void Contracts_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
		{
			LegalIdentity identity;

			if (e.SignatoryIdentityId == this.TagProfile.LegalIdentity?.Id)
				identity = this.TagProfile.LegalIdentity;
			else
			{
				(bool succeeded, LegalIdentity li) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetLegalIdentity(e.SignatoryIdentityId));

				if (succeeded && !(li is null))
					identity = li;
				else
					return;
			}

			if (identity is null)
			{
				this.LogService.LogWarning(GetType().Name + "." + nameof(Contracts_PetitionForSignatureReceived) + "() - identity is missing or cannot be retrieved, ignore.");
				return;
			}

			if (identity.State == IdentityState.Compromised || identity.State == IdentityState.Rejected)
				await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionSignatureResponse(e.SignatoryIdentityId, e.ContentToSign, new byte[0], e.PetitionId, e.RequestorFullJid, false));
			else
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					if (this.TagProfile.IsCompleteOrWaitingForValidation())
						await this.NavigationService.GoToAsync(nameof(PetitionSignaturePage), new PetitionSignatureNavigationArgs(e.RequestorIdentity, e.RequestorFullJid, e.SignatoryIdentityId, e.ContentToSign, e.PetitionId, e.Purpose));
				});
			}
		}

		private void Contracts_PetitionedSmartContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				if (!e.Response || e.RequestedContract is null)
					await this.UiSerializer.DisplayAlert(AppResources.Message, AppResources.PetitionToViewContractWasDenied, AppResources.Ok);
				else
				{
					await this.NavigationService.GoToAsync(nameof(ViewContractPage),
						new ViewContractNavigationArgs(e.RequestedContract, false));
				}
			});
		}

		private async void Contracts_PetitionForSmartContractReceived(object sender, ContractPetitionEventArgs e)
		{
			(bool succeeded, Contract contract) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetContract(e.RequestedContractId));

			if (!succeeded)
				return;

			if (contract.State == ContractState.Deleted || contract.State == ContractState.Rejected)
				await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionContractResponse(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false));
			else
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(PetitionContractPage),
						new PetitionContractNavigationArgs(e.RequestorIdentity, e.RequestorFullJid, contract, e.PetitionId, e.Purpose));
				});
			}
		}

		private async void Contracts_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
		{
			if (!e.Response || e.RequestedIdentity is null)
				await this.UiSerializer.DisplayAlert(AppResources.Message, AppResources.PetitionToViewLegalIdentityWasDenied, AppResources.Ok);
			else
			{
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(e.RequestedIdentity, null));
				});
			}
		}

		private async void Contracts_PetitionedPeerReviewResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				if (!e.Response)
					await this.UiSerializer.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasRejected, AppResources.Ok);
				else
				{
					StringBuilder xml = new();
					this.TagProfile.LegalIdentity.Serialize(xml, true, true, true, true, true, true, true);
					byte[] data = Encoding.UTF8.GetBytes(xml.ToString());
					bool? result;

					try
					{
						result = this.XmppService.Contracts.ValidateSignature(e.RequestedIdentity, data, e.Signature);
					}
					catch (Exception ex)
					{
						await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
						return;
					}

					this.UiSerializer.BeginInvokeOnMainThread(async () =>
					{
						if (!result.HasValue || !result.Value)
							await this.UiSerializer.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasBeenRejectedDueToSignatureError, AppResources.Ok);
						else
						{
							(bool succeeded, LegalIdentity legalIdentity) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.AddPeerReviewIdAttachment(this.TagProfile.LegalIdentity, e.RequestedIdentity, e.Signature));

							if (succeeded)
							{
								await this.UiSerializer.DisplayAlert(AppResources.PeerReviewAccepted, AppResources.APeerReviewYouhaveRequestedHasBeenAccepted, AppResources.Ok);
							}
						}
					});
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}

		}

		private async void Contracts_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			try
			{
				if (this.XmppService.IsOnline && this.XmppService.IsOnline)
				{
					if (!(this.TagProfile.LegalIdentity is null) && this.TagProfile.IsCompleteOrWaitingForValidation())
					{
						string id = this.TagProfile.LegalIdentity.Id;
						await Task.Delay(Constants.Timeouts.XmppInit);
						DownloadLegalIdentityInternal(id);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private async void Contracts_ContractProposalReceived(object sender, ContractProposalEventArgs e)
		{
			Contract contract;
			bool succeeded;

			(succeeded, contract) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetContract(e.ContractId));
			if (!succeeded || contract is null)
				return;     // Contract not available.

			if (contract.State != ContractState.Approved && contract.State != ContractState.BeingSigned)
				return;     // Not in a state to be signed.

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				if (this.TagProfile.IsCompleteOrWaitingForValidation())
				{
					await this.NavigationService.GoToAsync(nameof(ViewContractPage),
						new ViewContractNavigationArgs(contract, false, e.Role, e.MessageText));
				}
			});
		}

		#endregion

		protected virtual async void DownloadLegalIdentityInternal(string legalId)
		{
			// Run asynchronously so we're not blocking startup UI.
			try
			{
				await DownloadLegalIdentity(legalId);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		protected async Task DownloadLegalIdentity(string legalId)
		{
			bool isConnected =
				!(this.XmppService is null) &&
				await this.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect) &&
				this.XmppService.IsOnline;

			if (!isConnected)
				return;

			(bool succeeded, LegalIdentity identity) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.GetLegalIdentity(legalId), displayAlert: false);
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
							AppResources.Accept, AppResources.Cancel);

						if (Response)
						{
							try
							{
								identity = await this.XmppService.Contracts.ObsoleteLegalIdentity(identity.Id);
							}
							catch (Exception ex)
							{
								this.LogService.LogException(ex);
							}

							this.TagProfile.RevokeLegalIdentity(identity);
							gotoRegistrationPage = true;
						}
						else
						{
							await App.EvaluateDatabaseDiff("ChangesSinceLast.xml", false, true);	// TODO: To be removed when issue with lost keys has been solved
							await App.Stop();
							return;
						}
					}
					else
					{
						await App.EvaluateDatabaseDiff("ChangesSinceLast.xml", false, false);   // TODO: To be removed when issue with lost keys has been solved
						this.TagProfile.SetLegalIdentity(identity);
					}

					if (gotoRegistrationPage)
					{
						await this.NavigationService.GoToAsync(nameof(RegistrationPage));

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

		public async Task OpenLegalIdentity(string legalId, string purpose)
		{
			try
			{
				LegalIdentity identity = await this.XmppService.Contracts.GetLegalIdentity(legalId);
				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					await this.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(identity, null));
				});
			}
			catch (ForbiddenException)
			{
				// This happens if you try to view someone else's legal identity.
				// When this happens, try to send a petition to view it instead.
				// Normal operation. Should not be logged.

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.PetitionIdentity(legalId, Guid.NewGuid().ToString(), purpose));
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

		public async Task OpenContract(string contractId, string purpose)
		{
			try
			{
				Contract contract = await this.XmppService.Contracts.GetContract(contractId);

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					if (contract.CanActAsTemplate && contract.State == ContractState.Approved)
					{
						await this.SettingsService.SaveState(Constants.KeyPrefixes.ContractTemplatePrefix + contract.ContractId, DateTime.Now);
						await this.NavigationService.GoToAsync(nameof(NewContractPage), new NewContractNavigationArgs(contract));
					}
					else
					{
						await this.NavigationService.GoToAsync(nameof(ViewContractPage),
							  new ViewContractNavigationArgs(contract, false));
					}
				});
			}
			catch (ForbiddenException)
			{
				// This happens if you try to view someone else's contract.
				// When this happens, try to send a petition to view it instead.
				// Normal operation. Should not be logged.

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.PetitionContract(contractId, Guid.NewGuid().ToString(), purpose));
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
		/// <param name="request">Request string.</param>
		public async Task TagSignature(string request)
		{
			int i = request.IndexOf(',');
			if (i < 0)
				throw new InvalidOperationException("Invalid TAG Signature URI.");

			string JID = request.Substring(0, i);
			string Key = request[(i + 1)..];

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

	}
}