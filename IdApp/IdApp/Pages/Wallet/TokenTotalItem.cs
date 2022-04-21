using NeuroFeatures;

namespace IdApp.Pages.Wallet
{
	/// <summary>
	/// Encapsulates a <see cref="TokenTotal"/> object.
	/// </summary>
	public class TokenTotalItem
	{
		private readonly TokenTotal total;

		/// <summary>
		/// Encapsulates a <see cref="AccountEvent"/> object.
		/// </summary>
		/// <param name="Total">Token Total.</param>
		public TokenTotalItem(TokenTotal Total)
		{
			this.total = Total;
		}

		/// <summary>
		/// Currency for sub-total
		/// </summary>
		public string Currency => this.total.Currency;

		/// <summary>
		/// Number of tokens in sub-total
		/// </summary>
		public int NrTokens => this.total.NrTokens;

		/// <summary>
		/// Sub-total
		/// </summary>
		public decimal Total => this.total.Total;
	}
}
