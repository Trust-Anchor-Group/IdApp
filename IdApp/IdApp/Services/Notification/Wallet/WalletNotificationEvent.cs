using IdApp.Resx;
using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Abstract base class for wallet notification events.
	/// </summary>
	public abstract class WalletNotificationEvent : NotificationEvent
	{
		/// <summary>
		/// Abstract base class for wallet notification events.
		/// </summary>
		public WalletNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Abstract base class for wallet notification events.
		/// </summary>
		/// <param name="_">Event arguments</param>
		public WalletNotificationEvent(EventArgs _)
			: base()
		{
			this.Button = EventButton.Wallet;
			this.Timestamp = this.Received = DateTime.UtcNow;
		}

		/// <summary>
		/// Timestamp
		/// </summary>
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <param name="ServiceReferences"></param>
		/// <returns></returns>
		public override Task<string> GetCategoryIcon(ServiceReferences ServiceReferences)
		{
			return Task.FromResult<string>(FontAwesome.MoneyBill);
		}
	}
}
