﻿using IdApp.Services.Navigation;
using NeuroFeatures.Events;

namespace IdApp.Pages.Wallet.TokenEvents
{
    /// <summary>
    /// Holds navigation parameters containing the events of a token.
    /// </summary>
    public class TokenEventsNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TokenEventsNavigationArgs"/> class.
        /// </summary>
        public TokenEventsNavigationArgs() { }

        /// <summary>
        /// Creates a new instance of the <see cref="TokenEventsNavigationArgs"/> class.
        /// </summary>
        /// <param name="TokenId">Token ID</param>
        /// <param name="Events">Token events</param>
        public TokenEventsNavigationArgs(string TokenId, TokenEvent[] Events)
        {
            this.TokenId = TokenId;
            this.Events = Events;
        }
        
        /// <summary>
        /// Token ID
        /// </summary>
        public string TokenId { get; }

        /// <summary>
        /// Token events
        /// </summary>
        public TokenEvent[] Events { get; }
    }
}