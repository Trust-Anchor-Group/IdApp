using IdApp.Services.Navigation;

namespace IdApp.Pages.Wallet.TokenDetails
{
    /// <summary>
    /// Holds navigation parameters specific to eDaler URIs.
    /// </summary>
    public class TokenDetailsNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TokenDetailsNavigationArgs"/> class.
        /// </summary>
        /// <param name="Token">Information about a token.</param>
        public TokenDetailsNavigationArgs(TokenItem Token)
        {
            this.Token = Token;
        }
        
        /// <summary>
        /// Account event
        /// </summary>
        public TokenItem Token { get; }
    }
}