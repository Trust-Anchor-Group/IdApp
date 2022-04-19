using System;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using IdApp.Resx;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using NeuroFeatures.Events;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Wallet
{
	[Singleton]
	internal sealed class NeuroWallet : ServiceReferences, INeuroWallet
	{
		private EDalerClient eDalerClient;
		private NeuroFeaturesClient neuroFeaturesClient;
		private Balance lastBalance = null;
		private DateTime lastEvent = DateTime.MinValue;

		internal NeuroWallet()
			: base()
		{
		}

		#region e-Daler

		/// <summary>
		/// Reference to the e-Daler client implementing the e-Daler XMPP extension
		/// </summary>
		public EDalerClient EDalerClient
		{
			get
			{
				if (this.eDalerClient is null || this.eDalerClient.Client != this.XmppService.Xmpp)
				{
					if (!(this.eDalerClient is null))
						this.eDalerClient.BalanceUpdated -= EDalerClient_BalanceUpdated;

					this.eDalerClient = (this.XmppService as XmppService)?.EDalerClient;
					if (this.eDalerClient is null)
						throw new InvalidOperationException(AppResources.EDalerServiceNotFound);

					this.eDalerClient.BalanceUpdated += EDalerClient_BalanceUpdated;
				}

				return this.eDalerClient;
			}
		}

		private async Task EDalerClient_BalanceUpdated(object Sender, BalanceEventArgs e)
		{
			this.lastBalance = e.Balance;
			this.lastEvent = DateTime.Now;

			BalanceEventHandler h = this.BalanceUpdated;
			if (!(h is null))
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when balance has been updated
		/// </summary>
		public event BalanceEventHandler BalanceUpdated;

		/// <summary>
		/// Last reported balance
		/// </summary>
		public Balance LastBalance => this.lastBalance;

		/// <summary>
		/// Timepoint of last event.
		/// </summary>
		public DateTime LastEvent => this.lastEvent;

		/// <summary>
		/// Tries to parse an eDaler URI.
		/// </summary>
		/// <param name="Uri">URI string.</param>
		/// <param name="Parsed">Parsed eDaler URI, if successful.</param>
		/// <param name="Reason">Error message, if not able to parse URI.</param>
		/// <returns>If URI string could be parsed.</returns>
		public bool TryParseEDalerUri(string Uri, out EDalerUri Parsed, out string Reason)
		{
			return EDalerUri.TryParse(Uri, out Parsed, out Reason);
		}

		/// <summary>
		/// Tries to decrypt an encrypted private message.
		/// </summary>
		/// <param name="EncryptedMessage">Encrypted message.</param>
		/// <param name="PublicKey">Public key used.</param>
		/// <param name="TransactionId">ID of transaction containing the encrypted message.</param>
		/// <param name="RemoteEndpoint">Remote endpoint</param>
		/// <returns>Decrypted string, if successful, or null, if not.</returns>
		public async Task<string> TryDecryptMessage(byte[] EncryptedMessage, byte[] PublicKey, Guid TransactionId, string RemoteEndpoint)
		{
			try
			{
				return await this.EDalerClient.DecryptMessage(EncryptedMessage, PublicKey, TransactionId, RemoteEndpoint);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
				return string.Empty;
			}
		}

		/// <summary>
		/// Sends an eDaler URI to the eDaler service.
		/// </summary>
		/// <param name="Uri">eDaler URI</param>
		/// <returns>Transaction object containing information about the processed URI.</returns>
		public Task<Transaction> SendUri(string Uri)
		{
			return this.EDalerClient.SendEDalerUriAsync(Uri);
		}

		/// <summary>
		/// Gets account events available for the wallet.
		/// </summary>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <returns>Events found, and if more events are available.</returns>
		public Task<(AccountEvent[], bool)> GetAccountEventsAsync(int MaxCount)
		{
			return this.EDalerClient.GetAccountEventsAsync(MaxCount);
		}

		/// <summary>
		/// Gets account events available for the wallet.
		/// </summary>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <param name="From">From what point in time events should be returned.</param>
		/// <returns>Events found, and if more events are available.</returns>
		public Task<(AccountEvent[], bool)> GetAccountEventsAsync(int MaxCount, DateTime From)
		{
			return this.EDalerClient.GetAccountEventsAsync(MaxCount, From);
		}

		/// <summary>
		/// Gets the current account balance.
		/// </summary>
		/// <returns>Current account balance.</returns>
		public Task<Balance> GetBalanceAsync()
		{
			return this.EDalerClient.GetBalanceAsync();
		}

		/// <summary>
		/// Gets pending payments
		/// </summary>
		/// <returns>(Total amount, currency, items)</returns>
		public Task<(decimal, string, PendingPayment[])> GetPendingPayments()
		{
			return this.EDalerClient.GetPendingPayments();
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="ToBareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullPaymentUri(string ToBareJid, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays)
		{
			this.lastEvent = DateTime.Now;
			return this.EDalerClient.CreateFullPaymentUri(ToBareJid, Amount, AmountExtra, Currency, ValidNrDays);
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="ToBareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <param name="Message">Unencrypted message to send to recipient.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullPaymentUri(string ToBareJid, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays, string Message)
		{
			this.lastEvent = DateTime.Now;
			return this.EDalerClient.CreateFullPaymentUri(ToBareJid, Amount, AmountExtra, Currency, ValidNrDays, Message);
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullPaymentUri(LegalIdentity To, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays)
		{
			this.lastEvent = DateTime.Now;
			return this.EDalerClient.CreateFullPaymentUri(To, Amount, AmountExtra, Currency, ValidNrDays);
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <param name="PrivateMessage">Message to be sent to recipient. Message will be end-to-end encrypted.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullPaymentUri(LegalIdentity To, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays, string PrivateMessage)
		{
			this.lastEvent = DateTime.Now;
			return this.EDalerClient.CreateFullPaymentUri(To, Amount, AmountExtra, Currency, ValidNrDays, PrivateMessage);
		}

		/// <summary>
		/// Creates an incomplete PayMe-URI.
		/// </summary>
		/// <param name="BareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="Message">Message to be sent to recipient (not encrypted).</param>
		/// <returns>Incomplete PayMe-URI.</returns>
		public string CreateIncompletePayMeUri(string BareJid, decimal? Amount, decimal? AmountExtra, string Currency, string Message)
		{
			return this.EDalerClient.CreateIncompletePayMeUri(BareJid, Amount, AmountExtra, Currency, Message);
		}

		/// <summary>
		/// Creates an incomplete PayMe-URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="PrivateMessage">Message to be sent to recipient. Message will be end-to-end encrypted in payment.
		/// But the message will be unencrypted in the incomplete PeyMe URI.</param>
		/// <returns>Incomplete PayMe-URI.</returns>
		public string CreateIncompletePayMeUri(LegalIdentity To, decimal? Amount, decimal? AmountExtra, string Currency, string PrivateMessage)
		{
			return this.EDalerClient.CreateIncompletePayMeUri(To, Amount, AmountExtra, Currency, PrivateMessage);
		}

		#endregion

		#region Neuro-Features

		/// <summary>
		/// Reference to the Neuro-Features client implementing the Neuro-Features XMPP extension
		/// </summary>
		public NeuroFeaturesClient NeuroFeaturesClient
		{
			get
			{
				if (this.neuroFeaturesClient is null || this.neuroFeaturesClient.Client != this.XmppService.Xmpp)
				{
					if (!(this.neuroFeaturesClient is null))
					{
						this.neuroFeaturesClient.TokenAdded -= NeuroFeaturesClient_TokenAdded;
						this.neuroFeaturesClient.TokenRemoved -= NeuroFeaturesClient_TokenRemoved;
					}

					this.neuroFeaturesClient = (this.XmppService as XmppService)?.NeuroFeaturesClient;
					if (this.neuroFeaturesClient is null)
						throw new InvalidOperationException(AppResources.NeuroFeaturesServiceNotFound);

					this.neuroFeaturesClient.TokenAdded += NeuroFeaturesClient_TokenAdded;
					this.neuroFeaturesClient.TokenRemoved += NeuroFeaturesClient_TokenRemoved;
				}

				return this.neuroFeaturesClient;
			}
		}

		private async Task NeuroFeaturesClient_TokenRemoved(object Sender, TokenEventArgs e)
		{
			TokenEventHandler h = this.TokenRemoved;
			if (!(h is null))
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a token has been removed from the wallet.
		/// </summary>
		public event TokenEventHandler TokenRemoved;

		private async Task NeuroFeaturesClient_TokenAdded(object Sender, TokenEventArgs e)
		{
			TokenEventHandler h = this.TokenAdded;
			if (!(h is null))
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when a token has been added to the wallet.
		/// </summary>
		public event TokenEventHandler TokenAdded;

		/// <summary>
		/// Gets available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<TokensEventArgs> GetTokens()
		{
			return this.GetTokens(0, int.MaxValue);
		}

		/// <summary>
		/// Gets a section of available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<TokensEventArgs> GetTokens(int Offset, int MaxCount)
		{
			return this.NeuroFeaturesClient.GetTokensAsync(Offset, MaxCount);
		}

		/// <summary>
		/// Gets references to available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<string[]> GetTokenReferences()
		{
			return this.GetTokenReferences(0, int.MaxValue);
		}

		/// <summary>
		/// Gets references to a section of available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<string[]> GetTokenReferences(int Offset, int MaxCount)
		{
			return this.NeuroFeaturesClient.GetTokenReferencesAsync(Offset, MaxCount);
		}

		/// <summary>
		/// Gets the value totals of tokens available in the wallet, grouped and ordered by currency.
		/// </summary>
		/// <returns>Response with tokens.</returns>
		public Task<TokenTotalsEventArgs> GetTotals()
		{
			return this.NeuroFeaturesClient.GetTotalsAsync();
		}

		/// <summary>
		/// Gets a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <returns>Token</returns>
		public Task<Token> GetToken(string TokenId)
		{
			return this.NeuroFeaturesClient.GetTokenAsync(TokenId);
		}

		/// <summary>
		/// Gets events relating to a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <returns>Token events.</returns>
		public Task<TokenEvent[]> GetTokenEvents(string TokenId)
		{
			return this.GetTokenEvents(TokenId, 0, int.MaxValue);
		}

		/// <summary>
		/// Gets events relating to a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="Offset">Offset </param>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <returns>Token events.</returns>
		public Task<TokenEvent[]> GetTokenEvents(string TokenId, int Offset, int MaxCount)
		{
			return this.NeuroFeaturesClient.GetEventsAsync(TokenId, Offset, MaxCount);
		}

		/// <summary>
		/// Adds a text note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="TextNote">Text Note</param>
		public Task AddTextNote(string TokenId, string TextNote)
		{
			return this.AddTextNote(TokenId, TextNote, false);
		}

		/// <summary>
		/// Adds a text note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="TextNote">Text Note</param>
		/// <param name="Personal">If the text note contains personal information. (default=false).
		/// 
		/// Note: Personal notes are deleted when ownership of token is transferred.</param>
		public Task AddTextNote(string TokenId, string TextNote, bool Personal)
		{
			return this.NeuroFeaturesClient.AddTextNoteAsync(TokenId, TextNote, Personal);
		}

		/// <summary>
		/// Adds a xml note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="XmlNote">Xml Note</param>
		public Task AddXmlNote(string TokenId, string XmlNote)
		{
			return this.AddXmlNote(TokenId, XmlNote, false);
		}

		/// <summary>
		/// Adds a xml note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="XmlNote">Xml Note</param>
		/// <param name="Personal">If the xml note contains personal information. (default=false).
		/// 
		/// Note: Personal notes are deleted when ownership of token is transferred.</param>
		public Task AddXmlNote(string TokenId, string XmlNote, bool Personal)
		{
			return this.NeuroFeaturesClient.AddXmlNoteAsync(TokenId, XmlNote, Personal);
		}

		#endregion

	}
}