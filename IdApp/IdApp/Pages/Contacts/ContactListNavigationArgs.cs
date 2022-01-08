using IdApp.Services;
using IdApp.Services.Navigation;
using System.Threading.Tasks;

namespace IdApp.Pages.Contacts
{
    /// <summary>
    /// Actions to take when a contact has been selected.
    /// </summary>
    public enum SelectContactAction
    {
        /// <summary>
        /// Make a payment to contact.
        /// </summary>
        MakePayment,

        /// <summary>
        /// View the identity.
        /// </summary>
        ViewIdentity,

        /// <summary>
        /// Embed link to ID in chat
        /// </summary>
        Select
    }

    /// <summary>
    /// Holds navigation parameters specific to views displaying a list of contacts.
    /// </summary>
    public class ContactListNavigationArgs : NavigationArgs
    {
        private readonly string description;
        private readonly SelectContactAction action;
        private readonly TaskCompletionSource<ContactInfo> selection;

        /// <summary>
        /// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
        /// </summary>
        /// <param name="Description">Description presented to user.</param>
        /// <param name="Action">Action to take when a contact has been selected.</param>
        public ContactListNavigationArgs(string Description, SelectContactAction Action)
        {
            this.description = Description;
            this.action = Action;
        }

        /// <summary>
        /// Creates an instance of the <see cref="ContactListNavigationArgs"/> class.
        /// </summary>
        /// <param name="Description">Description presented to user.</param>
        /// <param name="Selection">Selection source, where selected item will be stored, or null if cancelled.</param>
        public ContactListNavigationArgs(string Description, TaskCompletionSource<ContactInfo> Selection)
            : this(Description, SelectContactAction.Select)
        {
            this.selection = Selection;
        }

        /// <summary>
        /// Description presented to user.
        /// </summary>
        public string Description => this.description;

        /// <summary>
        /// Action to take when a contact has been selected.
        /// </summary>
        public SelectContactAction Action => this.action;

        /// <summary>
        /// Selection source, if selecting identity.
        /// </summary>
        public TaskCompletionSource<ContactInfo> Selection => this.selection;
    }
}