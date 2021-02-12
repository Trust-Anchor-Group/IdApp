using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation
{
    /// <summary>
    /// Holds navigation parameters specific to views displaying a legal identity.
    /// </summary>
    public class ViewIdentityNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ViewIdentityNavigationArgs"/> class.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <param name="identityToReview">An identity to review, or <c>null</c>.</param>
        public ViewIdentityNavigationArgs(LegalIdentity identity, SignaturePetitionEventArgs identityToReview)
        {
            this.Identity = identity;
            this.IdentityToReview = identityToReview;
        }
        
        /// <summary>
        /// The identity to display.
        /// </summary>
        public LegalIdentity Identity { get; }
        /// <summary>
        /// The identity to review, if any.
        /// </summary>
        public SignaturePetitionEventArgs IdentityToReview { get; }
    }
}