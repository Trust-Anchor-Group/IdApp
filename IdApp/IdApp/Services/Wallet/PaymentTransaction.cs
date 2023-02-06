using System;
using System.Threading.Tasks;

namespace IdApp.Services.Wallet
{
	/// <summary>
	/// Maintains the status of an ongoing payment transaction to buy eDaler.
	/// </summary>
	public class PaymentTransaction
	{
		private readonly TaskCompletionSource<decimal?> result = new();
		private readonly string currency;

		/// <summary>
		/// Maintains the status of an ongoing payment transaction to buy eDaler.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <param name="Currency">Currency</param>
		public PaymentTransaction(string TransactionId, string Currency)
		{
			this.TransactionId = TransactionId;
			this.currency = Currency;
		}

		/// <summary>
		/// Transaction ID
		/// </summary>
		public string TransactionId { get; }

		/// <summary>
		/// Method called when a URL needs to be opened by the client.
		/// </summary>
		public async Task OpenUrl(string Url)
		{
			await App.OpenUrlAsync(Url);
		}

		/// <summary>
		/// Called when payment has failed.
		/// </summary>
		/// <param name="Message">Error message</param>
		public void ErrorReported(string Message)
		{
			this.result.TrySetException(new Exception(Message));
		}

		/// <summary>
		/// Called when payment has completed.
		/// </summary>
		/// <param name="Amount">Amount</param>
		/// <param name="Currency">Currency</param>
		public void Completed(decimal Amount, string Currency)
		{
			if (string.Compare(this.currency, Currency, true) == 0)
				this.result.TrySetResult(Amount);
			else
				this.result.TrySetException(new Exception("Payment of " + Amount.ToString() + " " + Currency + " completed. Expected " + this.currency + "."));
		}

		/// <summary>
		/// Waits for the completion of the payment.
		/// </summary>
		/// <returns>Amount transferred</returns>
		public Task<decimal?> Wait()
		{
			return this.result.Task;
		}
	}
}
