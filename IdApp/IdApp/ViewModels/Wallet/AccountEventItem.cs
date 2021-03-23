using System;
using Xamarin.Forms;
using EDaler;

namespace IdApp.ViewModels.Wallet
{
	/// <summary>
	/// Encapsulates a <see cref="AccountEvent"/> object.
	/// </summary>
	public class AccountEventItem
	{
		private readonly AccountEvent accountEvent;
		private readonly MyWalletViewModel viewModel;

		/// <summary>
		/// Encapsulates a <see cref="AccountEvent"/> object.
		/// </summary>
		/// <param name="AccountEvent">Account event.</param>
		/// <param name="ViewModel">Current view model</param>
		public AccountEventItem(AccountEvent AccountEvent, MyWalletViewModel ViewModel)
		{
			this.accountEvent = AccountEvent;
			this.viewModel = ViewModel;
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

		/// <summary>
		/// Remote endpoint in transaction.
		/// </summary>
		public string Remote => this.accountEvent.Remote;

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
