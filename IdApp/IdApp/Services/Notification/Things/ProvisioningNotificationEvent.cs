﻿using IdApp.Services.Notification.Xmpp;
using System;
using Waher.Networking.XMPP.Provisioning;

namespace IdApp.Services.Notification.Things
{
	/// <summary>
	/// Abstract base class of provisioning notification events.
	/// </summary>
	public abstract class ProvisioningNotificationEvent : XmppNotificationEvent
	{
		/// <summary>
		/// Abstract base class of provisioning notification events.
		/// </summary>
		public ProvisioningNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Abstract base class of provisioning notification events.
		/// </summary>
		public ProvisioningNotificationEvent(QuestionEventArgs e)
			: base()
		{
			this.Category = e.JID + "|||";
			this.BareJid = e.JID;
			this.RemoteJid = e.RemoteJID;
			this.Key = e.Key;
			this.ProvisioningService = e.From;
			this.Received = DateTime.UtcNow;
			this.Button = EventButton.Things;
		}

		/// <summary>
		/// JID of remote entity wishing to perform a task.
		/// </summary>
		public string RemoteJid { get; set; }

		/// <summary>
		/// Question Key
		/// </summary>
		public string Key { get; set; }

		/// <summary>
		/// Provisioning Service sending the question.
		/// </summary>
		public string ProvisioningService { get; set; }

		/// <summary>
		/// If notification event should be deleted when openedd.
		/// </summary>
		public override bool DeleteWhenOpened => false;
	}
}
