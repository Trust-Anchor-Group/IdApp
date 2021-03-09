using IdApp.Navigation;
using IdApp.Views;
using IdApp.Views.Contracts;
using IdApp.Views.Registration;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
	[Singleton]
	internal class ContractOrchestratorService : LoadableService, IContractOrchestratorService
	{
		private readonly ITagProfile tagProfile;
		private readonly IUiDispatcher uiDispatcher;
		private readonly INeuronService neuronService;
		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private readonly INetworkService networkService;

		public ContractOrchestratorService(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
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
			{
				this.neuronService.Contracts.ConnectionStateChanged += Contracts_ConnectionStateChanged;
				this.neuronService.Contracts.PetitionForPeerReviewIdReceived += Contracts_PetitionForPeerReviewIdReceived;
				this.neuronService.Contracts.PetitionForIdentityReceived += Contracts_PetitionForIdentityReceived;
				this.neuronService.Contracts.PetitionForSignatureReceived += Contracts_PetitionForSignatureReceived;
				this.neuronService.Contracts.PetitionedContractResponseReceived += Contracts_PetitionedNeuronContractResponseReceived;
				this.neuronService.Contracts.PetitionForContractReceived += Contracts_PetitionForNeuronContractReceived;
				this.neuronService.Contracts.PetitionedIdentityResponseReceived += Contracts_PetitionedIdentityResponseReceived;
				this.neuronService.Contracts.PetitionedPeerReviewIdResponseReceived += Contracts_PetitionedPeerReviewResponseReceived;
				this.EndLoad(true);
			}
			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.neuronService.Contracts.ConnectionStateChanged -= Contracts_ConnectionStateChanged;
				this.neuronService.Contracts.PetitionForPeerReviewIdReceived -= Contracts_PetitionForPeerReviewIdReceived;
				this.neuronService.Contracts.PetitionForIdentityReceived -= Contracts_PetitionForIdentityReceived;
				this.neuronService.Contracts.PetitionForSignatureReceived -= Contracts_PetitionForSignatureReceived;
				this.neuronService.Contracts.PetitionedContractResponseReceived -= Contracts_PetitionedNeuronContractResponseReceived;
				this.neuronService.Contracts.PetitionForContractReceived -= Contracts_PetitionForNeuronContractReceived;
				this.neuronService.Contracts.PetitionedIdentityResponseReceived -= Contracts_PetitionedIdentityResponseReceived;
				this.neuronService.Contracts.PetitionedPeerReviewIdResponseReceived -= Contracts_PetitionedPeerReviewResponseReceived;
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private void Contracts_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
		{
			this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					if (this.tagProfile.IsCompleteOrWaitingForValidation())
					{
						await this.navigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(e.RequestorIdentity, e));
					}
				}
			);
		}

		private async void Contracts_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
		{
			LegalIdentity identity;

			if (e.RequestedIdentityId == this.tagProfile.LegalIdentity?.Id)
			{
				identity = this.tagProfile.LegalIdentity;
			}
			else
			{
				(bool succeeded, LegalIdentity li) = await this.networkService.TryRequest(() => this.neuronService.Contracts.GetLegalIdentity(e.RequestedIdentityId));
				if (succeeded && !(li is null))
				{
					identity = li;
				}
				else
				{
					return;
				}
			}

			if (identity is null)
			{
				this.logService.LogWarning($"{GetType().Name}.{nameof(Contracts_PetitionForIdentityReceived)}() - identity is missing or cannot be retrieved, ignore.");
				return;
			}

			if (identity.State == IdentityState.Compromised ||
				identity.State == IdentityState.Rejected)
			{
				await this.networkService.TryRequest(() => this.neuronService.Contracts.SendPetitionIdentityResponse(e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false));
			}
			else
			{
				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					if (this.tagProfile.IsCompleteOrWaitingForValidation())
					{
						await this.navigationService.GoToAsync(nameof(PetitionIdentityPage), new PetitionIdentityNavigationArgs(e.RequestorIdentity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose));
					}
				});
			}
		}

		private async void Contracts_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
		{
			LegalIdentity identity;

			if (e.SignatoryIdentityId == this.tagProfile.LegalIdentity?.Id)
			{
				identity = this.tagProfile.LegalIdentity;
			}
			else
			{
				(bool succeeded, LegalIdentity li) = await this.networkService.TryRequest(() => this.neuronService.Contracts.GetLegalIdentity(e.SignatoryIdentityId));
				if (succeeded && !(li is null))
				{
					identity = li;
				}
				else
				{
					return;
				}
			}

			if (identity is null)
			{
				this.logService.LogWarning($"{GetType().Name}.{nameof(Contracts_PetitionForSignatureReceived)}() - identity is missing or cannot be retrieved, ignore.");
				return;
			}

			if (identity.State == IdentityState.Compromised ||
				identity.State == IdentityState.Rejected)
			{
				await this.networkService.TryRequest(() => this.neuronService.Contracts.SendPetitionSignatureResponse(e.SignatoryIdentityId, e.ContentToSign, new byte[0], e.PetitionId, e.RequestorFullJid, false));
			}
			else
			{
				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					if (this.tagProfile.IsCompleteOrWaitingForValidation())
					{
						await this.navigationService.GoToAsync(nameof(PetitionSignaturePage), new PetitionSignatureNavigationArgs(e.RequestorIdentity, e.RequestorFullJid, e.SignatoryIdentityId, e.ContentToSign, e.PetitionId, e.Purpose));
					}
				});
			}
		}

		private void Contracts_PetitionedNeuronContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
		{
			this.uiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				if (!e.Response || e.RequestedContract is null)
				{
					await this.uiDispatcher.DisplayAlert(AppResources.Message, AppResources.PetitionToViewContractWasDenied, AppResources.Ok);
				}
				else
				{
					await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(e.RequestedContract, false));
				}
			});
		}

		private async void Contracts_PetitionForNeuronContractReceived(object sender, ContractPetitionEventArgs e)
		{
			(bool succeeded, Contract contract) = await this.networkService.TryRequest(() => this.neuronService.Contracts.GetContract(e.RequestedContractId));

			if (!succeeded)
				return;

			if (contract.State == ContractState.Deleted ||
				contract.State == ContractState.Rejected)
			{
				await this.networkService.TryRequest(() => this.neuronService.Contracts.SendPetitionContractResponse(e.RequestedContractId, e.PetitionId, e.RequestorFullJid, false));
			}
			else
			{
				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					await this.navigationService.GoToAsync(nameof(PetitionContractPage), new PetitionContractNavigationArgs(e.RequestorIdentity, e.RequestorFullJid, contract, e.PetitionId, e.Purpose));
				});
			}
		}

		private async void Contracts_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
		{
			if (!e.Response || e.RequestedIdentity is null)
			{
				await this.uiDispatcher.DisplayAlert(AppResources.Message, AppResources.PetitionToViewLegalIdentityWasDenied, AppResources.Ok);
			}
			else
			{
				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					await this.navigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(e.RequestedIdentity, null));
				});
			}
		}

		private async void Contracts_PetitionedPeerReviewResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				if (!e.Response)
				{
					await this.uiDispatcher.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasRejected, AppResources.Ok);
				}
				else
				{
					StringBuilder xml = new StringBuilder();
					tagProfile.LegalIdentity.Serialize(xml, true, true, true, true, true, true, true);
					byte[] data = Encoding.UTF8.GetBytes(xml.ToString());
					bool? result;

					try
					{
						result = this.neuronService.Contracts.ValidateSignature(e.RequestedIdentity, data, e.Signature);
					}
					catch (Exception ex)
					{
						await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message);
						return;
					}

					this.uiDispatcher.BeginInvokeOnMainThread(async () =>
					{
						if (!result.HasValue || !result.Value)
						{
							await this.uiDispatcher.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasBeenRejectedDueToSignatureError, AppResources.Ok);
						}
						else
						{
							(bool succeeded, LegalIdentity legalIdentity) = await this.networkService.TryRequest(() => this.neuronService.Contracts.AddPeerReviewIdAttachment(tagProfile.LegalIdentity, e.RequestedIdentity, e.Signature));
							if (succeeded)
							{
								await this.uiDispatcher.DisplayAlert(AppResources.PeerReviewAccepted, AppResources.APeerReviewYouhaveRequestedHasBeenAccepted, AppResources.Ok);
							}
						}
					});
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}

		}

		private async void Contracts_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			if (this.neuronService.IsOnline && this.neuronService.Contracts.IsOnline && e.State == XmppState.Connected)
			{
				if (!(this.tagProfile.LegalIdentity is null) && this.tagProfile.IsCompleteOrWaitingForValidation())
				{
					string id = this.tagProfile.LegalIdentity.Id;
					await Task.Delay(Constants.Timeouts.XmppInit);
					DownloadLegalIdentityInternal(id);
				}
			}
		}

		#endregion

		protected virtual void DownloadLegalIdentityInternal(string legalId)
		{
			// Run asynchronously so we're not blocking startup UI.
			_ = DownloadLegalIdentity(legalId);
		}

		protected async Task DownloadLegalIdentity(string legalId)
        {
            bool isConnected = !(this.neuronService is null) &&
                               await this.neuronService.WaitForConnectedState(Constants.Timeouts.XmppConnect)
                               && this.neuronService.Contracts.IsOnline;

            if (!isConnected)
				return;

			(bool succeeded, LegalIdentity identity) = await this.networkService.TryRequest(() => this.neuronService.Contracts.GetLegalIdentity(legalId), displayAlert: false);
			if (succeeded)
			{
				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					string userMessage = null;
					bool gotoRegistrationPage = false;
					if (identity.State == IdentityState.Compromised)
					{
						userMessage = AppResources.YourLegalIdentityHasBeenCompromised;
						this.tagProfile.CompromiseLegalIdentity(identity);
						gotoRegistrationPage = true;
					}
					else if (identity.State == IdentityState.Obsoleted)
					{
						userMessage = AppResources.YourLegalIdentityHasBeenObsoleted;
						this.tagProfile.RevokeLegalIdentity(identity);
						gotoRegistrationPage = true;
					}
					else if (identity.State == IdentityState.Approved && !await this.neuronService.Contracts.HasPrivateKey(identity.Id))
					{
						userMessage = AppResources.YourLegalIdentityHasInvalidOrMissingKeys;
						try
						{
							identity = await this.neuronService.Contracts.ObsoleteLegalIdentity(identity.Id);
						}
						catch (Exception ex)
						{
							this.logService.LogException(ex);
						}

						this.tagProfile.RevokeLegalIdentity(identity);
						gotoRegistrationPage = true;
					}
					else
					{
						this.tagProfile.SetLegalIdentity(identity);
					}

					if (gotoRegistrationPage)
					{
						await this.navigationService.GoToAsync(nameof(RegistrationPage));

						// After navigating to the registration page, show the user why this happened.
						if (!string.IsNullOrWhiteSpace(userMessage))
						{
							// Do a begin invoke here so the page animation has time to finish,
							// and the view model loads state et.c. before showing the alert.
							// This gives a better UX experience.
							this.uiDispatcher.BeginInvokeOnMainThread(async () =>
							{
								await this.uiDispatcher.DisplayAlert(AppResources.YourLegalIdentity, userMessage);
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
				LegalIdentity identity = await this.neuronService.Contracts.GetLegalIdentity(legalId);
				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					await this.navigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(identity, null));
				});
			}
			catch (ForbiddenException)
			{
				// This happens if you try to view someone else's legal identity.
				// When this happens, try to send a petition to view it instead.
				// Normal operation. Should not be logged.

				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					bool succeeded = await this.networkService.TryRequest(() => this.neuronService.Contracts.PetitionIdentity(legalId, Guid.NewGuid().ToString(), purpose));
					if (succeeded)
					{
						await this.uiDispatcher.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToTheOwner);
					}
				});
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				await this.uiDispatcher.DisplayAlert(AppResources.AnErrorHasOccurred, ex);
			}
		}

		public async Task OpenContract(string contractId, string purpose)
		{
			try
			{
				Contract contract = await this.neuronService.Contracts.GetContract(contractId);

				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					if (contract.CanActAsTemplate && contract.State == ContractState.Approved)
						await this.navigationService.GoToAsync(nameof(NewContractPage), new NewContractNavigationArgs(contract));
					else
						await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(contract, false));
				});
			}
			catch (ForbiddenException)
			{
				// This happens if you try to view someone else's contract.
				// When this happens, try to send a petition to view it instead.
				// Normal operation. Should not be logged.

				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					bool succeeded = await this.networkService.TryRequest(() => this.neuronService.Contracts.PetitionContract(contractId, Guid.NewGuid().ToString(), purpose));
					if (succeeded)
					{
						await this.uiDispatcher.DisplayAlert(AppResources.PetitionSent, AppResources.APetitionHasBeenSentToTheContract);
					}
				});
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				await this.uiDispatcher.DisplayAlert(AppResources.AnErrorHasOccurred, ex);
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
			string Key = request.Substring(i + 1);

			LegalIdentity ID = this.tagProfile?.LegalIdentity;
			if (ID is null)
				throw new InvalidOperationException("No Legal ID selected.");

			if (ID.State != IdentityState.Approved)
				throw new InvalidOperationException("Legal ID not approved.");

			string IdRef = this.tagProfile?.LegalIdentity?.Id ?? string.Empty;

			StringBuilder Xml = new StringBuilder();

			Xml.Append("<ql xmlns='https://tagroot.io/schema/Signature' key='");
			Xml.Append(XML.Encode(Key));
			Xml.Append("' legalId='");
			Xml.Append(XML.Encode(IdRef));
			Xml.Append("'/>");

			if (!this.neuronService.IsOnline)
				throw new InvalidOperationException("App is not connected to the network.");

			await this.neuronService.Xmpp.IqSetAsync(JID, Xml.ToString());
		}

	}
}