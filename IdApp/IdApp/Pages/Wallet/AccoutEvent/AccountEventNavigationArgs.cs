using IdApp.Services.Navigation;

namespace IdApp.Pages.Wallet.AccountEvent
{
    /// <summary>
    /// Holds navigation parameters specific to eDaler URIs.
    /// </summary>
    public class AccountEventNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AccountEventNavigationArgs"/> class.
        /// </summary>
        /// <param name="Event">Information about an account event.</param>
        public AccountEventNavigationArgs(AccountEventItem Event)
        {
            this.Event = Event;
        }
        
        /// <summary>
        /// Account event
        /// </summary>
        public AccountEventItem Event { get; }
    }
}