using IdApp.Services.Navigation;

namespace IdApp.Pages.Contacts.Chat
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying a list of contacts.
    /// </summary>
    public class ChatNavigationArgs : NavigationArgs
    {
        private readonly string bareJid;
        private readonly string friendlyName;

        /// <summary>
        /// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
        /// </summary>
        /// <param name="BareJid">Bare JID of remote chat party</param>
        /// <param name="FriendlyName">Friendly name</param>
        public ChatNavigationArgs(string BareJid, string FriendlyName)
        {
            this.bareJid = BareJid;
            this.friendlyName = FriendlyName;
        }

        /// <summary>
        /// Bare JID of remote chat party
        /// </summary>
        public string BareJid => this.bareJid;

        /// <summary>
        /// Friendly name
        /// </summary>
        public string FriendlyName => this.friendlyName;
    }
}