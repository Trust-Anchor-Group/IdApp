﻿using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
    /// <summary>
    /// Orchestrates eDaler operations.
    /// </summary>
    [DefaultImplementation(typeof(EDalerOrchestratorService))]
    public interface IEDalerOrchestratorService : ILoadableService
    {
        /// <summary>
        /// eDaler URI scanned.
        /// </summary>
        /// <param name="uri">eDaler URI.</param>
        Task EDalerUri(string uri);
    }
}