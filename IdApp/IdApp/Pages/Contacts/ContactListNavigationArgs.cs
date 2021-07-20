using Tag.Neuron.Xamarin.Services;

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
        ViewIdentity
    }

    /// <summary>
    /// Holds navigation parameters specific to views displaying a list of contacts.
    /// </summary>
    public class ContactListNavigationArgs : NavigationArgs
    {
        private readonly string description;
        private readonly SelectContactAction action;

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
        /// Description presented to user.
        /// </summary>
        public string Description => this.description;

        /// <summary>
        /// Action to take when a contact has been selected.
        /// </summary>
        public SelectContactAction Action => this.action;
    }
}