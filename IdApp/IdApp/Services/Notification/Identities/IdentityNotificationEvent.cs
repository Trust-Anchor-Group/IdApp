using IdApp.Resx;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Identities
{
	/// <summary>
	/// Abstract base class of Identity notification events.
	/// </summary>
	public abstract class IdentityNotificationEvent : NotificationEvent
	{
		private LegalIdentity identity;

		/// <summary>
		/// Abstract base class of Identity notification events.
		/// </summary>
		public IdentityNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Abstract base class of Identity notification events.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public IdentityNotificationEvent(SignaturePetitionEventArgs e)
			: base()
		{
			this.identity = e.RequestorIdentity;
			this.RequestorFullJid = e.RequestorFullJid;
			this.SignatoryIdentityId = e.SignatoryIdentityId;
			this.PetitionId = e.PetitionId;
			this.Purpose = e.Purpose;
			this.ContentToSign = e.ContentToSign;
		}

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

		/// <summary>
		/// XML of identity.
		/// </summary>
		public string IdentityXml { get; set; }

		/// <summary>
		/// Gets a parsed identity.
		/// </summary>
		/// <returns>Parsed identity</returns>
		public LegalIdentity Identity
		{
			get
			{
				if (this.identity is null && !string.IsNullOrEmpty(this.IdentityXml))
				{
					XmlDocument Doc = new();
					Doc.LoadXml(this.IdentityXml);

					this.identity = LegalIdentity.Parse(Doc.DocumentElement);
				}

				return this.identity;
			}

			set
			{
				this.identity = value;

				if (value is null)
					this.IdentityXml = null;
				else
				{
					StringBuilder Xml = new();
					value.Serialize(Xml, true, true, true, true, true, true, true);
					this.IdentityXml = Xml.ToString();
				}
			}
		}

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <param name="ServiceReferences"></param>
		/// <returns></returns>
		public override Task<string> GetCategoryIcon(ServiceReferences ServiceReferences)
		{
			return Task.FromResult<string>(FontAwesome.Passport);
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override Task<string> GetCategoryDescription(ServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			return Task.FromResult<string>(ContactInfo.GetFriendlyName(Identity));
		}

	}
}
