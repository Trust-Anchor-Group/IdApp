using IdApp.Services.Navigation;

namespace IdApp.Pages.Contacts.Chat
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying a list of contacts.
    /// </summary>
    public class ChatNavigationArgs : NavigationArgs
    {
        private readonly string legalId;
        private readonly string bareJid;
        private readonly string friendlyName;

        /// <summary>
        /// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
        /// </summary>
        /// <param name="LegalId">Legal ID, if available.</param>
        /// <param name="BareJid">Bare JID of remote chat party</param>
        /// <param name="FriendlyName">Friendly name</param>
        public ChatNavigationArgs(string LegalId, string BareJid, string FriendlyName)
        {
            this.legalId = LegalId;
            this.bareJid = BareJid;
            this.friendlyName = FriendlyName;
        }

        /// <summary>
        /// Legal ID, if available.
        /// </summary>
        public string LegalId => this.legalId;

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