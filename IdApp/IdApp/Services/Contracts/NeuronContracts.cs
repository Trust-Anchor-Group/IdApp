using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IdApp.Pages.Registration.RegisterIdentity;
using IdApp.Services.Neuron;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Runtime.Inventory;
using Waher.Runtime.Temporary;

namespace IdApp.Services.Contracts
{
	[Singleton]
	internal sealed class NeuronContracts : ServiceReferences, INeuronContracts
	{
		private ContractsClient contractsClient;
		private HttpFileUploadClient fileUploadClient;

		internal NeuronContracts()
		{
		}

		/// <summary>
		/// Contracts client
		/// </summary>
		public ContractsClient ContractsClient
		{
			get
			{
				if (this.contractsClient is null || this.contractsClient.Client != this.NeuronService.Xmpp)
				{
					if (!(this.contractsClient is null))
					{
						this.contractsClient.IdentityUpdated -= ContractsClient_IdentityUpdated;
						this.contractsClient.PetitionForIdentityReceived -= ContractsClient_PetitionForIdentityReceived;
						this.contractsClient.PetitionedIdentityResponseReceived -= ContractsClient_PetitionedIdentityResponseReceived;
						this.contractsClient.PetitionForContractReceived -= ContractsClient_PetitionForContractReceived;
						this.contractsClient.PetitionedContractResponseReceived -= ContractsClient_PetitionedContractResponseReceived;
						this.contractsClient.PetitionForSignatureReceived -= ContractsClient_PetitionForSignatureReceived;
						this.contractsClient.PetitionedSignatureResponseReceived -= ContractsClient_PetitionedSignatureResponseReceived;
						this.contractsClient.PetitionForPeerReviewIDReceived -= ContractsClient_PetitionForPeerReviewIdReceived;
						this.contractsClient.PetitionedPeerReviewIDResponseReceived -= ContractsClient_PetitionedPeerReviewIdResponseReceived;
						this.contractsClient.ContractProposalReceived -= ContractsClient_ContractProposalReceived;
					}

					this.contractsClient = (this.NeuronService as NeuronService)?.ContractsClient;
					if (this.contractsClient is null)
						throw new InvalidOperationException(AppResources.LegalServiceNotFound);

					this.contractsClient.IdentityUpdated += ContractsClient_IdentityUpdated;
					this.contractsClient.PetitionForIdentityReceived += ContractsClient_PetitionForIdentityReceived;
					this.contractsClient.PetitionedIdentityResponseReceived += ContractsClient_PetitionedIdentityResponseReceived;
					this.contractsClient.PetitionForContractReceived += ContractsClient_PetitionForContractReceived;
					this.contractsClient.PetitionedContractResponseReceived += ContractsClient_PetitionedContractResponseReceived;
					this.contractsClient.PetitionForSignatureReceived += ContractsClient_PetitionForSignatureReceived;
					this.contractsClient.PetitionedSignatureResponseReceived += ContractsClient_PetitionedSignatureResponseReceived;
					this.contractsClient.PetitionForPeerReviewIDReceived += ContractsClient_PetitionForPeerReviewIdReceived;
					this.contractsClient.PetitionedPeerReviewIDResponseReceived += ContractsClient_PetitionedPeerReviewIdResponseReceived;
					this.contractsClient.ContractProposalReceived += ContractsClient_ContractProposalReceived;
				}

				return this.contractsClient;
			}
		}

		/// <summary>
		/// HTTP File Upload client
		/// </summary>
		public HttpFileUploadClient FileUploadClient
		{
			get
			{
				if (this.fileUploadClient is null || this.fileUploadClient.Client != this.NeuronService.Xmpp)
				{
					this.fileUploadClient = this.NeuronService?.FileUploadClient;
					if (this.fileUploadClient is null)
						throw new InvalidOperationException(AppResources.FileUploadServiceNotFound);
				}

				return this.fileUploadClient;
			}
		}

		public Task PetitionContract(string contractId, string petitionId, string purpose)
		{
			return this.ContractsClient.PetitionContractAsync(contractId, petitionId, purpose);
		}

		public Task<Contract> GetContract(string contractId)
		{
			return this.ContractsClient.GetContractAsync(contractId);
		}

		public Task<KeyValuePair<string, TemporaryFile>> GetContractAttachmentAsync(string url)
		{
			return this.ContractsClient.GetAttachmentAsync(url, SignWith.LatestApprovedId);
		}

		public Task<KeyValuePair<string, TemporaryFile>> GetAttachment(string url, SignWith signWith, TimeSpan timeout)
		{
			return this.ContractsClient.GetAttachmentAsync(url, signWith, (int)timeout.TotalMilliseconds);
		}

		public Task<Contract> CreateContract(
			string templateId,
			Part[] parts,
			Parameter[] parameters,
			ContractVisibility visibility,
			ContractParts partsMode,
			Duration duration,
			Duration archiveRequired,
			Duration archiveOptional,
			DateTime? signAfter,
			DateTime? signBefore,
			bool canActAsTemplate)
		{
			return this.ContractsClient.CreateContractAsync(templateId, parts, parameters, visibility, partsMode, duration, archiveRequired, archiveOptional, signAfter, signBefore, canActAsTemplate);
		}

		public Task<Contract> DeleteContract(string contractId)
		{
			return this.ContractsClient.DeleteContractAsync(contractId);
		}

		public async Task<Contract[]> GetCreatedContracts()
		{
			List<Contract> Result = new List<Contract>();
			ContractsEventArgs Contracts;
			int Offset = 0;
			int Nr;

			do
			{
				Contracts = await this.ContractsClient.GetCreatedContractsAsync(Offset, 20);
				Result.AddRange(Contracts.Contracts);
				Nr = Contracts.Contracts.Length + Contracts.References.Length;
				Offset += Nr;
			}
			while (Nr == 20);

			return Result.ToArray();
		}

		public async Task<Contract[]> GetSignedContracts()
		{
			List<Contract> Result = new List<Contract>();
			ContractsEventArgs Contracts;
			int Offset = 0;
			int Nr;

			do
			{
				Contracts = await this.ContractsClient.GetSignedContractsAsync(Offset, 20);
				Result.AddRange(Contracts.Contracts);
				Nr = Contracts.Contracts.Length + Contracts.References.Length;
				Offset += Nr;
			}
			while (Nr == 20);

			return Result.ToArray();
		}

		/// <summary>
		/// Gets the id's of contract templates used.
		/// </summary>
		/// <returns>Id's of contract templates, together with the last time they were used.</returns>
		public async Task<KeyValuePair<DateTime, string>[]> GetContractTemplateIds()
		{
			List<KeyValuePair<DateTime, string>> Result = new List<KeyValuePair<DateTime, string>>();
			string Prefix = Constants.KeyPrefixes.ContractTemplatePrefix;
			int PrefixLen = Prefix.Length;

			foreach ((string Key, DateTime LastUsed) in await this.SettingsService.RestoreStateWhereKeyStartsWith<DateTime>(Prefix))
				Result.Add(new KeyValuePair<DateTime, string>(LastUsed, Key.Substring(PrefixLen)));

			return Result.ToArray();
		}

		public Task<Contract> SignContract(Contract contract, string role, bool transferable)
		{
			return this.ContractsClient.SignContractAsync(contract, role, transferable);
		}

		public Task<Contract> ObsoleteContract(string contractId)
		{
			return this.ContractsClient.ObsoleteContractAsync(contractId);
		}

		public async Task<LegalIdentity> AddLegalIdentity(RegisterIdentityModel model, params LegalIdentityAttachment[] attachments)
		{
			await this.ContractsClient.GenerateNewKeys();

			LegalIdentity identity = await this.ContractsClient.ApplyAsync(model.ToProperties(this.NeuronService));

			foreach (var a in attachments)
			{
				HttpFileUploadEventArgs e2 = await this.FileUploadClient.RequestUploadSlotAsync(Path.GetFileName(a.Filename), a.ContentType, a.ContentLength);
				if (!e2.Ok)
					throw e2.StanzaError ?? new Exception(e2.ErrorText);

				await e2.PUT(a.Data, a.ContentType, (int)Constants.Timeouts.UploadFile.TotalMilliseconds);

				byte[] signature = await this.ContractsClient.SignAsync(a.Data, SignWith.CurrentKeys);

				identity = await this.ContractsClient.AddLegalIdAttachmentAsync(identity.Id, e2.GetUrl, signature);
			}

			return identity;
		}

		/// <summary>
		/// Checks if the client has access to the private keys of the specified legal identity.
		/// </summary>
		/// <param name="legalIdentityId">The id of the legal identity.</param>
		/// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
		/// <returns>If private keys are available.</returns>
		public async Task<bool> HasPrivateKey(string legalIdentityId, XmppClient client = null)
		{
			if (client is null)
				return await this.ContractsClient.HasPrivateKey(legalIdentityId);
			else
			{
				using (ContractsClient cc = new ContractsClient(client, this.TagProfile.LegalJid))
				{
					if (!await cc.LoadKeys(false))
						return false;

					return await cc.HasPrivateKey(legalIdentityId);
				}
			}
		}

		/// <summary>
		/// Gets a specific legal identity.
		/// </summary>
		/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
		/// <returns>Legal identity object</returns>
		public async Task<LegalIdentity> GetLegalIdentity(string legalIdentityId)
		{
			ContactInfo Info = await ContactInfo.FindByLegalId(legalIdentityId);
			if (!(Info is null) && !(Info.LegalIdentity is null))
				return Info.LegalIdentity;

			return await this.ContractsClient.GetLegalIdentityAsync(legalIdentityId);
		}

		/// <summary>
		/// Checks if a legal identity is in the contacts list.
		/// </summary>
		/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
		/// <returns>If the legal identity is in the contacts list.</returns>
		public async Task<bool> IsContact(string legalIdentityId)
		{
			ContactInfo Info = await ContactInfo.FindByLegalId(legalIdentityId);
			return (!(Info is null) && !(Info.LegalIdentity is null));
		}


		public Task PetitionIdentity(string legalId, string petitionId, string purpose)
		{
			return this.ContractsClient.PetitionIdentityAsync(legalId, petitionId, purpose);
		}

		public Task SendPetitionIdentityResponse(string legalId, string petitionId, string requestorFullJid, bool response)
		{
			return this.ContractsClient.PetitionIdentityResponseAsync(legalId, petitionId, requestorFullJid, response);
		}

		public Task SendPetitionContractResponse(string contractId, string petitionId, string requestorFullJid, bool response)
		{
			return this.ContractsClient.PetitionContractResponseAsync(contractId, petitionId, requestorFullJid, response);
		}

		public Task SendPetitionSignatureResponse(string legalId, byte[] content, byte[] signature, string petitionId, string requestorFullJid, bool response)
		{
			return this.ContractsClient.PetitionSignatureResponseAsync(legalId, content, signature, petitionId, requestorFullJid, response);
		}

		public Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity identity, LegalIdentity reviewerLegalIdentity, byte[] peerSignature)
		{
			return this.ContractsClient.AddPeerReviewIDAttachment(identity, reviewerLegalIdentity, peerSignature);
		}

		public Task PetitionPeerReviewId(string legalId, LegalIdentity identity, string petitionId, string purpose)
		{
			return this.ContractsClient.PetitionPeerReviewIDAsync(legalId, identity, petitionId, purpose);
		}

		public Task<byte[]> Sign(byte[] data, SignWith signWith)
		{
			return this.ContractsClient.SignAsync(data, signWith);
		}

		public async Task<LegalIdentity[]> GetLegalIdentities(XmppClient client = null)
		{
			if (client is null)
				return await this.ContractsClient.GetLegalIdentitiesAsync();
			else
			{
				using (ContractsClient cc = new ContractsClient(client, this.TagProfile.LegalJid))  // No need to load keys for this operation.
				{
					return await cc.GetLegalIdentitiesAsync();
				}
			}
		}

		public bool? ValidateSignature(LegalIdentity legalIdentity, byte[] data, byte[] signature)
		{
			return this.ContractsClient.ValidateSignature(legalIdentity, data, signature);
		}

		public Task<LegalIdentity> ObsoleteLegalIdentity(string legalIdentityId)
		{
			return this.ContractsClient.ObsoleteLegalIdentityAsync(legalIdentityId);
		}

		public Task<LegalIdentity> CompromiseLegalIdentity(string legalIdentityId)
		{
			return this.ContractsClient.CompromisedLegalIdentityAsync(legalIdentityId);
		}

		#region Events

		public event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;

		private void OnLegalIdentityChanged(LegalIdentityChangedEventArgs e)
		{
			LegalIdentityChanged?.Invoke(this, e);
		}

		public event EventHandler<LegalIdentityPetitionEventArgs> PetitionForIdentityReceived;

		private void OnPetitionForIdentityReceived(LegalIdentityPetitionEventArgs e)
		{
			PetitionForIdentityReceived?.Invoke(this, e);
		}

		public event EventHandler<SignaturePetitionEventArgs> PetitionForSignatureReceived;

		private void OnPetitionForSignatureReceived(SignaturePetitionEventArgs e)
		{
			PetitionForSignatureReceived?.Invoke(this, e);
		}

		public event EventHandler<LegalIdentityPetitionResponseEventArgs> PetitionedIdentityResponseReceived;

		private void OnPetitionedIdentityResponseReceived(LegalIdentityPetitionResponseEventArgs e)
		{
			PetitionedIdentityResponseReceived?.Invoke(this, e);
		}

		public event EventHandler<ContractPetitionEventArgs> PetitionForContractReceived;

		private void OnPetitionForContractReceived(ContractPetitionEventArgs e)
		{
			PetitionForContractReceived?.Invoke(this, e);
		}

		public event EventHandler<ContractPetitionResponseEventArgs> PetitionedContractResponseReceived;

		private void OnPetitionedContractResponseReceived(ContractPetitionResponseEventArgs e)
		{
			PetitionedContractResponseReceived?.Invoke(this, e);
		}

		public event EventHandler<SignaturePetitionEventArgs> PetitionForPeerReviewIdReceived;

		private void OnPetitionForPeerReviewIdReceived(SignaturePetitionEventArgs e)
		{
			PetitionForPeerReviewIdReceived?.Invoke(this, e);
		}

		public event EventHandler<SignaturePetitionResponseEventArgs> PetitionedPeerReviewIdResponseReceived;

		private void OnPetitionedPeerReviewIdResponseReceived(SignaturePetitionResponseEventArgs e)
		{
			PetitionedPeerReviewIdResponseReceived?.Invoke(this, e);
		}

		public event EventHandler<SignaturePetitionResponseEventArgs> SignaturePetitionResponseReceived;

		private void OnPetitionedSignatureResponseReceived(SignaturePetitionResponseEventArgs e)
		{
			SignaturePetitionResponseReceived?.Invoke(this, e);
		}

		public event EventHandler<ContractProposalEventArgs> ContractProposalReceived;

		private void OnContractProposalReceived(ContractProposalEventArgs e)
		{
			ContractProposalReceived?.Invoke(this, e);
		}

		#endregion

		#region Event Handlers

		private async Task ContractsClient_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
		{
			try
			{
				this.OnPetitionedContractResponseReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		private async Task ContractsClient_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
		{
			try
			{
				this.OnPetitionForContractReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		private async Task ContractsClient_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
		{
			try
			{
				this.OnPetitionedIdentityResponseReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		private async Task ContractsClient_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
		{
			try
			{
				this.OnPetitionForIdentityReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		private async Task ContractsClient_PetitionedPeerReviewIdResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				this.OnPetitionedPeerReviewIdResponseReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		private async Task ContractsClient_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
		{
			try
			{
				this.OnPetitionForPeerReviewIdReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		private async Task ContractsClient_PetitionedSignatureResponseReceived(object sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				this.OnPetitionedSignatureResponseReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		private async Task ContractsClient_PetitionForSignatureReceived(object sender, SignaturePetitionEventArgs e)
		{
			try
			{
				this.OnPetitionForSignatureReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		private async Task ContractsClient_IdentityUpdated(object sender, LegalIdentityEventArgs e)
		{
			if (this.TagProfile.LegalIdentity is null ||
				this.TagProfile.LegalIdentity.Id == e.Identity.Id ||
				this.TagProfile.LegalIdentity.Created < e.Identity.Created)
			{
				try
				{
					OnLegalIdentityChanged(new LegalIdentityChangedEventArgs(e.Identity));
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
				}
			}
		}

		private async Task ContractsClient_ContractProposalReceived(object Sender, ContractProposalEventArgs e)
		{
			try
			{
				this.OnContractProposalReceived(e);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
			}
		}

		#endregion

		public bool IsOnline => !(this.ContractsClient is null) && this.ContractsClient.Client.State == XmppState.Connected;

		public bool FileUploadIsSupported
		{
			get
			{
				try
				{
					return this.TagProfile.FileUploadIsSupported && !(this.FileUploadClient is null) && this.FileUploadClient.HasSupport;
				}
				catch (Exception ex)
				{
					this.LogService?.LogException(ex);
					return false;
				}
			}
		}
	}
}