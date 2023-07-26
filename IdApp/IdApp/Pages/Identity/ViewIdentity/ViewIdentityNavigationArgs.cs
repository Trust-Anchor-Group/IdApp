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
		/// <param name="RequestorFullJid">Full JID of requestor.</param>
		/// <param name="SignatoryIdentityId">Legal identity of signatory.</param>
		/// <param name="PetitionId">ID of petition.</param>
		/// <param name="Purpose">Purpose message to display.</param>
		/// <param name="ContentToSign">Content to sign.</param>
		public ViewIdentityNavigationArgs(LegalIdentity identity, string RequestorFullJid, string SignatoryIdentityId,
			string PetitionId, string Purpose, byte[] ContentToSign)
		{
			this.Identity = identity;
			this.RequestorIdentity = identity;
			this.RequestorFullJid = RequestorFullJid;
			this.SignatoryIdentityId = SignatoryIdentityId;
			this.PetitionId = PetitionId;
			this.Purpose = Purpose;
			this.ContentToSign = ContentToSign;
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
