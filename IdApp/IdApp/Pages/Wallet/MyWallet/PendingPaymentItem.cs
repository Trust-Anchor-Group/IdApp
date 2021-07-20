using System;

namespace IdApp.Pages.Wallet.MyWallet
{
	/// <summary>
	/// Encapsulates a <see cref="PendingPayment"/> object.
	/// </summary>
	public class PendingPaymentItem
	{
		private readonly EDaler.PendingPayment pendingPayment;

		/// <summary>
		/// Encapsulates a <see cref="PendingPayment"/> object.
		/// </summary>
		/// <param name="PendingPayment">Pending payment.</param>
		public PendingPaymentItem(EDaler.PendingPayment PendingPayment)
		{
			this.pendingPayment = PendingPayment;
		}

		/// <summary>
		/// Associated transaction ID
		/// </summary>
		public Guid Id => this.pendingPayment.Id;

		/// <summary>
		/// When pending payment expires
		/// </summary>
		public DateTime Expires => this.pendingPayment.Expires;

		/// <summary>
		/// String representation of <see cref="Expires"/>
		/// </summary>
		public string ExpiresStr => string.Format(AppResources.ExpiresAt, this.Expires.ToShortDateString());

		/// <summary>
		/// Currency of pending payment
		/// </summary>
		public string Currency => this.pendingPayment.Currency.Value;

		/// <summary>
		/// Amount pending
		/// </summary>
		public decimal Amount => this.pendingPayment.Amount;

		/// <summary>
		/// Sender of payment
		/// </summary>
		public string From => this.pendingPayment.From;

		/// <summary>
		/// Recipient of payment
		/// </summary>
		public string To => this.pendingPayment.To;

		/// <summary>
		/// Corresponding eDaler URI.
		/// </summary>
		public string Uri => this.pendingPayment.Uri;

	}
}
