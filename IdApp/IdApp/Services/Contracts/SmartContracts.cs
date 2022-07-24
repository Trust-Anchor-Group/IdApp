using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IdApp.Pages.Registration.RegisterIdentity;
using IdApp.Resx;
using IdApp.Services.Xmpp;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Runtime.Temporary;

namespace IdApp.Services.Contracts
{
	[Singleton]
	internal sealed class SmartContracts : ServiceReferences, ISmartContracts
	{
		private readonly Dictionary<CaseInsensitiveString, DateTime> lastContractEvent = new();
		private ContractsClient contractsClient;
		private HttpFileUploadClient fileUploadClient;

		internal SmartContracts()
		{
		}

		/// <summary>
		/// Contracts client
		/// </summary>
		public ContractsClient ContractsClient
		{
			get
			{
				if (this.contractsClient is null || this.contractsClient.Client != this.XmppService.Xmpp)
				{
					if (!(this.contractsClient is null))
					{
						this.contractsClient.IdentityUpdated -= this.ContractsClient_IdentityUpdated;
						this.contractsClient.PetitionForIdentityReceived -= this.ContractsClient_PetitionForIdentityReceived;
						this.contractsClient.PetitionedIdentityResponseReceived -= this.ContractsClient_PetitionedIdentityResponseReceived;
						this.contractsClient.PetitionForContractReceived -= this.ContractsClient_PetitionForContractReceived;
						this.contractsClient.PetitionedContractResponseReceived -= this.ContractsClient_PetitionedContractResponseReceived;
						this.contractsClient.PetitionForSignatureReceived -= this.ContractsClient_PetitionForSignatureReceived;
						this.contractsClient.PetitionedSignatureResponseReceived -= this.ContractsClient_PetitionedSignatureResponseReceived;
						this.contractsClient.PetitionForPeerReviewIDReceived -= this.ContractsClient_PetitionForPeerReviewIdReceived;
						this.contractsClient.PetitionedPeerReviewIDResponseReceived -= this.ContractsClient_PetitionedPeerReviewIdResponseReceived;
						this.contractsClient.ContractProposalReceived -= this.ContractsClient_ContractProposalReceived;
						this.contractsClient.ContractUpdated -= this.ContractsClient_ContractUpdatedOrSigned;
						this.contractsClient.ContractSigned -= this.ContractsClient_ContractUpdatedOrSigned;
					}

					this.contractsClient = (this.XmppService as XmppService)?.ContractsClient;
					if (this.contractsClient is null)
						throw new InvalidOperationException(AppResources.LegalServiceNotFound);

					this.contractsClient.IdentityUpdated += this.ContractsClient_IdentityUpdated;
					this.contractsClient.PetitionForIdentityReceived += this.ContractsClient_PetitionForIdentityReceived;
					this.contractsClient.PetitionedIdentityResponseReceived += this.ContractsClient_PetitionedIdentityResponseReceived;
					this.contractsClient.PetitionForContractReceived += this.ContractsClient_PetitionForContractReceived;
					this.contractsClient.PetitionedContractResponseReceived += this.ContractsClient_PetitionedContractResponseReceived;
					this.contractsClient.PetitionForSignatureReceived += this.ContractsClient_PetitionForSignatureReceived;
					this.contractsClient.PetitionedSignatureResponseReceived += this.ContractsClient_PetitionedSignatureResponseReceived;
					this.contractsClient.PetitionForPeerReviewIDReceived += this.ContractsClient_PetitionForPeerReviewIdReceived;
					this.contractsClient.PetitionedPeerReviewIDResponseReceived += this.ContractsClient_PetitionedPeerReviewIdResponseReceived;
					this.contractsClient.ContractProposalReceived += this.ContractsClient_ContractProposalReceived;
					this.contractsClient.ContractUpdated += this.ContractsClient_ContractUpdatedOrSigned;
					this.contractsClient.ContractSigned += this.ContractsClient_ContractUpdatedOrSigned;
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
				if (this.fileUploadClient is null || this.fileUploadClient.Client != this.XmppService.Xmpp)
				{
					this.fileUploadClient = this.XmppService?.FileUploadClient;
					if (this.fileUploadClient is null)
						throw new InvalidOperationException(AppResources.FileUploadServiceNotFound);
				}

				return this.fileUploadClient;
			}
		}

		/// <summary>
		/// Petitions a contract with the specified id and purpose.
		/// </summary>
		/// <param name="ContractId">The contract id.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="Purpose">The purpose.</param>
		public Task PetitionContract(CaseInsensitiveString ContractId, string PetitionId, string Purpose)
		{
			return this.ContractsClient.PetitionContractAsync(ContractId, PetitionId, Purpose);
		}

		/// <summary>
		/// Gets the contract with the specified id.
		/// </summary>
		/// <param name="ContractId">The contract id.</param>
		/// <returns>Smart Contract</returns>
		public Task<Contract> GetContract(CaseInsensitiveString ContractId)
		{
			return this.ContractsClient.GetContractAsync(ContractId);
		}

		/// <summary>
		/// Gets an attachment for a contract.
		/// </summary>
		/// <param name="Url">The url of the attachment.</param>
		/// <param name="Timeout">Max timeout allowed when retrieving an attachment.</param>
		/// <param name="SignWith">How the request is signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
		/// <returns>Content-Type, and attachment file.</returns>
		public Task<KeyValuePair<string, TemporaryFile>> GetAttachment(string Url, SignWith SignWith, TimeSpan Timeout)
		{
			return this.ContractsClient.GetAttachmentAsync(Url, SignWith, (int)Timeout.TotalMilliseconds);
		}

		/// <summary>
		/// Creates a new contract.
		/// </summary>
		/// <param name="TemplateId">The id of the contract template to use.</param>
		/// <param name="Parts">The individual contract parts.</param>
		/// <param name="Parameters">Contract parameters.</param>
		/// <param name="Visibility">The contract's visibility.</param>
		/// <param name="PartsMode">The contract's parts.</param>
		/// <param name="Duration">Duration of the contract.</param>
		/// <param name="ArchiveRequired">Required duration for contract archival.</param>
		/// <param name="ArchiveOptional">Optional duration for contract archival.</param>
		/// <param name="SignAfter">Timestamp of when the contract can be signed at the earliest.</param>
		/// <param name="SignBefore">Timestamp of when the contract can be signed at the latest.</param>
		/// <param name="CanActAsTemplate">Can this contract act as a template itself?</param>
		/// <returns>Smart Contract</returns>
		public Task<Contract> CreateContract(
			CaseInsensitiveString TemplateId,
			Part[] Parts,
			Parameter[] Parameters,
			ContractVisibility Visibility,
			ContractParts PartsMode,
			Duration Duration,
			Duration ArchiveRequired,
			Duration ArchiveOptional,
			DateTime? SignAfter,
			DateTime? SignBefore,
			bool CanActAsTemplate)
		{
			return this.ContractsClient.CreateContractAsync(TemplateId, Parts, Parameters, Visibility, PartsMode, Duration, ArchiveRequired, ArchiveOptional, SignAfter, SignBefore, CanActAsTemplate);
		}

		/// <summary>
		/// Deletes a contract.
		/// </summary>
		/// <param name="ContractId">The id of the contract to delete.</param>
		/// <returns>Smart Contract</returns>
		public Task<Contract> DeleteContract(CaseInsensitiveString ContractId)
		{
			return this.ContractsClient.DeleteContractAsync(ContractId);
		}

		/// <summary>
		/// Gets created contracts.
		/// </summary>
		/// <returns>Created contracts.</returns>
		public async Task<Contract[]> GetCreatedContracts()
		{
			List<Contract> Result = new();
			ContractsEventArgs Contracts;
			int Offset = 0;
			int Nr;

			do
			{
				Contracts = await this.ContractsClient.GetCreatedContractsAsync(Offset, 20);
				if (!Contracts.Ok)
					throw Contracts.StanzaError ?? new Exception("Unable to get created contracts.");

				Result.AddRange(Contracts.Contracts);
				Nr = Contracts.Contracts.Length + Contracts.References.Length;
				Offset += Nr;
			}
			while (Nr == 20);

			return Result.ToArray();
		}

		/// <summary>
		/// Gets signed contracts.
		/// </summary>
		/// <returns>Signed contracts.</returns>
		public async Task<Contract[]> GetSignedContracts()
		{
			List<Contract> Result = new();
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
		public async Task<KeyValuePair<DateTime, CaseInsensitiveString>[]> GetContractTemplateIds()
		{
			List<KeyValuePair<DateTime, CaseInsensitiveString>> Result = new();
			string Prefix = Constants.KeyPrefixes.ContractTemplatePrefix;
			int PrefixLen = Prefix.Length;

			foreach ((string Key, DateTime LastUsed) in await this.SettingsService.RestoreStateWhereKeyStartsWith<DateTime>(Prefix))
				Result.Add(new KeyValuePair<DateTime, CaseInsensitiveString>(LastUsed, Key[PrefixLen..]));

			return Result.ToArray();
		}

		/// <summary>
		/// Signs a given contract.
		/// </summary>
		/// <param name="Contract">The contract to sign.</param>
		/// <param name="Role">The role of the signer.</param>
		/// <param name="Transferable">Whether the contract is transferable or not.</param>
		/// <returns>Smart Contract</returns>
		public Task<Contract> SignContract(Contract Contract, string Role, bool Transferable)
		{
			return this.ContractsClient.SignContractAsync(Contract, Role, Transferable);
		}

		/// <summary>
		/// Obsoletes a contract.
		/// </summary>
		/// <param name="ContractId">The id of the contract to obsolete.</param>
		/// <returns>Smart Contract</returns>
		public Task<Contract> ObsoleteContract(CaseInsensitiveString ContractId)
		{
			return this.ContractsClient.ObsoleteContractAsync(ContractId);
		}

		/// <summary>
		/// Adds a legal identity.
		/// </summary>
		/// <param name="Model">The model holding all the values needed.</param>
		/// <param name="Attachments">The physical attachments to upload.</param>
		/// <returns>Legal Identity</returns>
		public async Task<LegalIdentity> AddLegalIdentity(RegisterIdentityModel Model, params LegalIdentityAttachment[] Attachments)
		{
			await this.ContractsClient.GenerateNewKeys();

			LegalIdentity identity = await this.ContractsClient.ApplyAsync(Model.ToProperties(this.XmppService));

			foreach (LegalIdentityAttachment a in Attachments)
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
		public async Task<bool> HasPrivateKey(CaseInsensitiveString legalIdentityId, XmppClient client = null)
		{
			if (client is null)
				return await this.ContractsClient.HasPrivateKey(legalIdentityId);
			else
			{
				using ContractsClient cc = new(client, this.TagProfile.LegalJid);

				if (!await cc.LoadKeys(false))
					return false;

				return await cc.HasPrivateKey(legalIdentityId);
			}
		}

		/// <summary>
		/// Gets a specific legal identity.
		/// </summary>
		/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
		/// <returns>Legal identity object</returns>
		public async Task<LegalIdentity> GetLegalIdentity(CaseInsensitiveString legalIdentityId)
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
		public async Task<bool> IsContact(CaseInsensitiveString legalIdentityId)
		{
			ContactInfo Info = await ContactInfo.FindByLegalId(legalIdentityId);
			return (!(Info is null) && !(Info.LegalIdentity is null));
		}

		/// <summary>
		/// Petitions a legal identity.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="Purpose">The purpose of the petitioning.</param>
		public async Task PetitionIdentity(CaseInsensitiveString LegalId, string PetitionId, string Purpose)
		{
			await this.ContractsClient.AuthorizeAccessToIdAsync(this.TagProfile.LegalIdentity.Id, LegalId, true);
			await this.ContractsClient.PetitionIdentityAsync(LegalId, PetitionId, Purpose);
		}

		/// <summary>
		/// Sends a response to a petitioning identity request.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="RequestorFullJid">The full Jid of the requestor.</param>
		/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
		public Task SendPetitionIdentityResponse(CaseInsensitiveString LegalId, string PetitionId, string RequestorFullJid, bool Response)
		{
			return this.ContractsClient.PetitionIdentityResponseAsync(LegalId, PetitionId, RequestorFullJid, Response);
		}

		/// <summary>
		/// Sends a response to a petitioning contract request.
		/// </summary>
		/// <param name="ContractId">The id of the contract.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="RequestorFullJid">The full Jid of the requestor.</param>
		/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
		public Task SendPetitionContractResponse(CaseInsensitiveString ContractId, string PetitionId, string RequestorFullJid, bool Response)
		{
			return this.ContractsClient.PetitionContractResponseAsync(ContractId, PetitionId, RequestorFullJid, Response);
		}

		/// <summary>
		/// Sends a response to a petitioning signature request.
		/// </summary>
		/// <param name="LegalId">Legal Identity petitioned.</param>
		/// <param name="Content">Content to be signed.</param>
		/// <param name="Signature">Digital signature of content, made by the legal identity.</param>
		/// <param name="PetitionId">A petition identifier. This identifier will follow the petition, and can be used
		/// to identify the petition request.</param>
		/// <param name="RequestorFullJid">Full JID of requestor.</param>
		/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
		public Task SendPetitionSignatureResponse(CaseInsensitiveString LegalId, byte[] Content, byte[] Signature, string PetitionId, string RequestorFullJid, bool Response)
		{
			return this.ContractsClient.PetitionSignatureResponseAsync(LegalId, Content, Signature, PetitionId, RequestorFullJid, Response);
		}

		/// <summary>
		/// Adds an attachment for the peer review.
		/// </summary>
		/// <param name="Identity">The identity to which the attachment should be added.</param>
		/// <param name="ReviewerLegalIdentity">The identity of the reviewer.</param>
		/// <param name="PeerSignature">The raw signature data.</param>
		/// <returns>Legal Identity</returns>
		public Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity Identity, LegalIdentity ReviewerLegalIdentity, byte[] PeerSignature)
		{
			return this.ContractsClient.AddPeerReviewIDAttachment(Identity, ReviewerLegalIdentity, PeerSignature);
		}

		/// <summary>
		/// Sends a petition to a third-party to review a legal identity.
		/// </summary>
		/// <param name="LegalId">The legal id to petition.</param>
		/// <param name="Identity">The legal id to peer review.</param>
		/// <param name="PetitionId">The petition id.</param>
		/// <param name="Purpose">The purpose.</param>
		public async Task PetitionPeerReviewId(CaseInsensitiveString LegalId, LegalIdentity Identity, string PetitionId, string Purpose)
		{
			await this.ContractsClient.AuthorizeAccessToIdAsync(Identity.Id, LegalId, true);
			await this.ContractsClient.PetitionPeerReviewIDAsync(LegalId, Identity, PetitionId, Purpose);
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
				using ContractsClient cc = new(client, this.TagProfile.LegalJid);  // No need to load keys for this operation.
				return await cc.GetLegalIdentitiesAsync();
			}
		}

		public bool? ValidateSignature(LegalIdentity legalIdentity, byte[] data, byte[] signature)
		{
			return this.ContractsClient.ValidateSignature(legalIdentity, data, signature);
		}

		public Task<LegalIdentity> ObsoleteLegalIdentity(CaseInsensitiveString legalIdentityId)
		{
			return this.ContractsClient.ObsoleteLegalIdentityAsync(legalIdentityId);
		}

		public Task<LegalIdentity> CompromiseLegalIdentity(CaseInsensitiveString legalIdentityId)
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
					this.OnLegalIdentityChanged(new LegalIdentityChangedEventArgs(e.Identity));
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

		private Task ContractsClient_ContractUpdatedOrSigned(object Sender, ContractReferenceEventArgs e)
		{
			lock (this.lastContractEvent)
			{
				this.lastContractEvent[e.ContractId] = DateTime.Now;
			}

			return Task.CompletedTask;
		}

		public DateTime GetTimeOfLastContraceEvent(CaseInsensitiveString ContractId)
		{
			lock (this.lastContractEvent)
			{
				if (this.lastContractEvent.TryGetValue(ContractId, out DateTime TP))
					return TP;
				else
					return DateTime.MinValue;
			}
		}

	}
}
