using IdApp.Pages.Wallet.MyWallet.ObjectModels;
using IdApp.Services.Navigation;
using System.Threading.Tasks;

namespace IdApp.Pages.Wallet.MyTokens
{
	/// <summary>
	/// Holds navigation parameters specific to the eDaler wallet.
	/// </summary>
	public class MyTokensNavigationArgs : NavigationArgs
    {
        private readonly TaskCompletionSource<TokenItem> selected;

        /// <summary>
        /// Creates a new instance of the <see cref="MyTokensNavigationArgs"/> class.
        /// </summary>
        public MyTokensNavigationArgs()
        {
            this.selected = new();
        }

        /// <summary>
        /// Task completion source, waiting for a response from the user.
        /// </summary>
        public TaskCompletionSource<TokenItem> Selected => this.selected;

        /// <summary>
        /// Waits for the token to be selected; null is returned if the user goes back.
        /// </summary>
        /// <returns>Selected token, or null if user cancels, by going back.</returns>
        public Task<TokenItem> WaitForTokenSelection()
        {
            return this.selected?.Task ?? Task.FromResult<TokenItem>(null);
        }
    }
}