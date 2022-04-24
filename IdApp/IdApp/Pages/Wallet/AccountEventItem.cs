using IdApp.Resx;
using System;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet
{
	/// <summary>
	/// Encapsulates a <see cref="AccountEvent"/> object.
	/// </summary>
	public class AccountEventItem : IItemGroup
	{
		private readonly EDaler.AccountEvent accountEvent;
		private readonly MyWallet.MyWalletViewModel viewModel;
		private readonly string friendlyName;

		/// <summary>
		/// Encapsulates a <see cref="AccountEvent"/> object.
		/// </summary>
		/// <param name="AccountEvent">Account event.</param>
		/// <param name="ViewModel">Current view model</param>
		/// <param name="FriendlyName">Friendly name of remote entity.</param>
		public AccountEventItem(EDaler.AccountEvent AccountEvent, MyWallet.MyWalletViewModel ViewModel, string FriendlyName)
		{
			this.accountEvent = AccountEvent;
			this.viewModel = ViewModel;
			this.friendlyName = FriendlyName;
		}

		/// <summary>
		/// Balance after event.
		/// </summary>
		public decimal Balance => this.accountEvent.Balance;

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
		public string UniqueName => TransactionId.ToString();

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
					return AppResources.Yesterday + ", " + this.Timestamp.ToLongTimeString();
				else
					return this.Timestamp.ToShortDateString() + ", " + this.Timestamp.ToLongTimeString();
			}
		}
	}
}
