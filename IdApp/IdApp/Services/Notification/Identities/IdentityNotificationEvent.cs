﻿using IdApp.Extensions;
using IdApp.Resx;
using IdApp.Services.UI.Photos;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;

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
			this.Identity = e.RequestorIdentity;
			this.Category = e.RequestorIdentity.Id;
			this.RequestorFullJid = e.RequestorFullJid;
			this.SignatoryIdentityId = e.SignatoryIdentityId;
			this.PetitionId = e.PetitionId;
			this.Purpose = e.Purpose;
			this.ContentToSign = e.ContentToSign;
			this.Button = EventButton.Contracts;
			this.Received = DateTime.UtcNow;
		}

		/// <summary>
		/// Abstract base class of Identity notification events.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public IdentityNotificationEvent(LegalIdentityPetitionEventArgs e)
			: base()
		{
			this.Identity = e.RequestorIdentity;
			this.Category = e.RequestorIdentity.Id;
			this.RequestorFullJid = e.RequestorFullJid;
			this.SignatoryIdentityId = e.RequestedIdentityId;
			this.PetitionId = e.PetitionId;
			this.Purpose = e.Purpose;
			this.ContentToSign = null;
			this.Button = EventButton.Contracts;
			this.Received = DateTime.UtcNow;
		}

		/// <summary>
		/// Abstract base class of Identity notification events.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public IdentityNotificationEvent(SignaturePetitionResponseEventArgs e)
			: base()
		{
			this.Identity = e.RequestedIdentity;
			this.Category = e.RequestedIdentity.Id;
			this.Button = EventButton.Contracts;
			this.PetitionId = e.PetitionId;
			this.ContentToSign = e.Signature;
			this.Received = DateTime.UtcNow;
		}

		/// <summary>
		/// Abstract base class of Identity notification events.
		/// </summary>
		/// <param name="e">Event arguments</param>
		public IdentityNotificationEvent(LegalIdentityPetitionResponseEventArgs e)
			: base()
		{
			this.Identity = e.RequestedIdentity;
			this.Category = e.RequestedIdentity.Id;
			this.PetitionId = e.PetitionId;
			this.Button = EventButton.Contracts;
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
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
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
		public override Task<string> GetCategoryIcon(IServiceReferences ServiceReferences)
		{
			return Task.FromResult<string>(FontAwesome.Passport);
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			return Task.FromResult(ContactInfo.GetFriendlyName(Identity));
		}

		/// <summary>
		/// Performs perparatory tasks, that will simplify opening the notification.
		/// </summary>
		public override async Task Prepare(IServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			if (Identity?.Attachments is not null)
			{
				foreach (Attachment Attachment in Identity.Attachments.GetImageAttachments())
				{
					try
					{
						await PhotosLoader.LoadPhoto(Attachment);
					}
					catch (Exception ex)
					{
						ServiceReferences.LogService.LogException(ex);
					}
				}
			}
		}

	}
}
