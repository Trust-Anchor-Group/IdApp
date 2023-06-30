using IdApp.Converters;
using IdApp.Services;
using IdApp.Services.Notification;
using System;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MyWallet.ObjectModels
{
	/// <summary>
	/// Encapsulates a <see cref="AccountEvent"/> object.
	/// </summary>
	public class AccountEventItem : IUniqueItem
	{
		private readonly IServiceReferences services;
		private readonly EDaler.AccountEvent accountEvent;
		private readonly MyWalletViewModel viewModel;
		private readonly string friendlyName;
		private bool? @new;
		private NotificationEvent[] notificationEvents;

		/// <summary>
		/// Encapsulates a <see cref="AccountEvent"/> object.
		/// </summary>
		/// <param name="AccountEvent">Account event.</param>
		/// <param name="ViewModel">Current view model</param>
		/// <param name="FriendlyName">Friendly name of remote entity.</param>
		/// <param name="NotificationEvents">Notification events.</param>
		/// <param name="Services">Service references.</param>
		public AccountEventItem(EDaler.AccountEvent AccountEvent, MyWalletViewModel ViewModel, string FriendlyName, NotificationEvent[] NotificationEvents,
			IServiceReferences Services)
		{
			this.accountEvent = AccountEvent;
			this.viewModel = ViewModel;
			this.friendlyName = FriendlyName;
			this.notificationEvents = NotificationEvents;
			this.services = Services;
		}

		/// <summary>
		/// Balance after event.
		/// </summary>
		public decimal Balance => this.accountEvent.Balance;

		/// <summary>
		/// Reserved after event.
		/// </summary>
		public decimal Reserved => this.accountEvent.Reserved;

		/// <summary>
		/// Balance change
		/// </summary>
		public decimal Change => this.accountEvent.Change;

		/// <summary>
		/// Timestamp of event
		/// </summary>
		public DateTime Timestamp => this.accountEvent.Timestamp;

		/// <summary>
		/// Transaction ID corresponding to event.
		/// </summary>
		public Guid TransactionId => this.accountEvent.TransactionId;

		/// <inheritdoc/>
		public string UniqueName => this.TransactionId.ToString();

		/// <summary>
		/// Remote endpoint in transaction.
		/// </summary>
		public string Remote => this.accountEvent.Remote;

		/// <summary>
		/// Friendly name of remote entity.
		/// </summary>
		public string FriendlyName => this.friendlyName;

		/// <summary>
		/// Any message associated with event.
		/// </summary>
		public string Message => this.accountEvent.Message;

		/// <summary>
		/// If the event has a message.
		/// </summary>
		public bool HasMessage => !string.IsNullOrEmpty(this.accountEvent.Message);

		/// <summary>
		/// Currency used for event.
		/// </summary>
		public string Currency => this.viewModel.Currency;

		/// <summary>
		/// Associated notification events
		/// </summary>
		public NotificationEvent[] NotificationEvents => this.notificationEvents;

		/// <summary>
		/// If the event item is new or not.
		/// </summary>
		public bool New
		{
			get
			{
				if (!this.@new.HasValue)
				{
					this.@new = this.notificationEvents.Length > 0;
					if (this.@new.Value)
					{
						NotificationEvent[] ToDelete = this.notificationEvents;

						this.notificationEvents = new NotificationEvent[0];

						Task.Run(() => this.services.NotificationService.DeleteEvents(ToDelete));
					}
				}

				return this.@new.Value;
			}
		}

		/// <summary>
		/// Color to use when displaying change.
		/// </summary>
		public Color TextColor
		{
			get
			{
				if (this.Change >= 0)
					return Color.Black;
				else
					return Color.Red;
			}
		}

		/// <summary>
		/// String representation of timestamp
		/// </summary>
		public string TimestampStr
		{
			get
			{
				DateTime Today = DateTime.Today;

				if (this.Timestamp.Date == Today)
					return this.Timestamp.ToLongTimeString();
				else if (this.Timestamp.Date == Today.AddDays(-1))
					return LocalizationResourceManager.Current["Yesterday"] + ", " + this.Timestamp.ToLongTimeString();
				else
					return this.Timestamp.ToShortDateString() + ", " + this.Timestamp.ToLongTimeString();
			}
		}

		/// <summary>
		/// Formatted string of any amount being reserved.
		/// </summary>
		public string ReservedSuffix
		{
			get
			{
				if (this.accountEvent.Reserved == 0)
					return string.Empty;

				return "+" + MoneyToString.ToString(this.accountEvent.Reserved);
			}
		}
	}
}
