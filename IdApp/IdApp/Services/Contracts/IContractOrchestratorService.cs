﻿using IdApp.Services.Tag;
using NeuroFeatures;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Contracts
{
    /// <summary>
    /// Orchestrates operations on contracts upon receiving certain events, like approving or rejecting other peers' review requests.
    /// Also keeps track of the <see cref="ITagProfile"/> for the current user, and applies the correct navigation should the legal identity be compromised or revoked.
    /// </summary>
    [DefaultImplementation(typeof(ContractOrchestratorService))]
    public interface IContractOrchestratorService : ILoadableService
    {
        /// <summary>
        /// Downloads the specified <see cref="LegalIdentity"/> and opens the corresponding page in the app to show it.
        /// </summary>
        /// <param name="legalId">The id of the legal identity to show.</param>
        /// <param name="purpose">The purpose to state if the identity can't be downloaded and needs to be petitioned instead.</param>
        Task OpenLegalIdentity(string legalId, string purpose);

		/// <summary>
		/// Downloads the specified <see cref="Contract"/> and opens the corresponding page in the app to show it.
		/// </summary>
		/// <param name="contractId">The id of the contract to show.</param>
		/// <param name="purpose">The purpose to state if the contract can't be downloaded and needs to be petitioned instead.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		Task OpenContract(string contractId, string purpose, Dictionary<string, object> ParameterValues);

        /// <summary>
        /// TAG Signature request scanned.
        /// </summary>
        /// <param name="request">Request string.</param>
        Task TagSignature(string request);
    }
}
