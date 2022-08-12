using IdApp.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Identity.ViewIdentity
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a legal identity.
	/// </summary>
	public class ViewIdentityNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ViewIdentityNavigationArgs"/> class.
		/// </summary>
		public ViewIdentityNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ViewIdentityNavigationArgs"/> class.
		/// </summary>
		/// <param name="identity">The identity.</param>
		public ViewIdentityNavigationArgs(LegalIdentity identity)
		{
			this.Identity = identity;
			this.RequestorIdentity = null;
			this.RequestorFullJid = null;
			this.SignatoryIdentityId = null;
			this.PetitionId = null;
			this.Purpose = null;
			this.ContentToSign = null;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ViewIdentityNavigationArgs"/> class.
		/// </summary>
		/// <param name="identity">The identity.</param>
		/// <param name="identityToReview">An identity to review, or <c>null</c>.</param>
		public ViewIdentityNavigationArgs(LegalIdentity identity, SignaturePetitionEventArgs identityToReview)
		{
			this.Identity = identity;
			this.RequestorIdentity = identityToReview.RequestorIdentity;
			this.RequestorFullJid = identityToReview.RequestorFullJid;
			this.SignatoryIdentityId = identityToReview.SignatoryIdentityId;
			this.PetitionId = identityToReview.PetitionId;
			this.Purpose = identityToReview.Purpose;
			this.ContentToSign = identityToReview.ContentToSign;
		}

		/// <summary>
		/// The identity to display.
		/// </summary>
		public LegalIdentity Identity { get; }

		/// <summary>
		/// Legal Identity of requesting entity.
		/// </summary>
		public LegalIdentity RequestorIdentity { get; }

		/// <summary>
		/// Full JID of requestor.
		/// </summary>
		public string RequestorFullJid { get; }

		/// <summary>
		/// Legal identity of petitioned signatory.
		/// </summary>
		public string SignatoryIdentityId { get; }

		/// <summary>
		/// Petition ID
		/// </summary>
		public string PetitionId { get; }

		/// <summary>
		/// Purpose
		/// </summary>
		public string Purpose { get; }

		/// <summary>
		/// Content to sign.
		/// </summary>
		public byte[] ContentToSign { get; }

	}
}
