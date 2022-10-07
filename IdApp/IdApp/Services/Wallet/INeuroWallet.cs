using System;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using NeuroFeatures;
using NeuroFeatures.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Wallet
{
	/// <summary>
	/// Adds support for eDaler wallets.
	/// </summary>
	[DefaultImplementation(typeof(NeuroWallet))]
	public interface INeuroWallet
	{
		#region e-Daler

		/// <summary>
		/// Tries to parse an eDaler URI.
		/// </summary>
		/// <param name="Uri">URI string.</param>
		/// <param name="Parsed">Parsed eDaler URI, if successful.</param>
		/// <param name="Reason">Error message, if not able to parse URI.</param>
		/// <returns>If URI string could be parsed.</returns>
		bool TryParseEDalerUri(string Uri, out EDalerUri Parsed, out string Reason);

		/// <summary>
		/// Tries to decrypt an encrypted private message.
		/// </summary>
		/// <param name="EncryptedMessage">Encrypted message.</param>
		/// <param name="PublicKey">Public key used.</param>
		/// <param name="TransactionId">ID of transaction containing the encrypted message.</param>
		/// <param name="RemoteEndpoint">Remote endpoint</param>
		/// <returns>Decrypted string, if successful, or null, if not.</returns>
		Task<string> TryDecryptMessage(byte[] EncryptedMessage, byte[] PublicKey, Guid TransactionId, string RemoteEndpoint);

		/// <summary>
		/// Sends an eDaler URI to the eDaler service.
		/// </summary>
		/// <param name="Uri">eDaler URI</param>
		/// <returns>Transaction object containing information about the processed URI.</returns>
		Task<Transaction> SendUri(string Uri);

		/// <summary>
		/// Event raised when balance has been updated.
		/// </summary>
		event BalanceEventHandler BalanceUpdated;

		/// <summary>
		/// Gets account events available for the wallet.
		/// </summary>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <returns>Events found, and if more events are available.</returns>
		Task<(AccountEvent[], bool)> GetAccountEventsAsync(int MaxCount);

		/// <summary>
		/// Gets account events available for the wallet.
		/// </summary>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <param name="From">From what point in time events should be returned.</param>
		/// <returns>Events found, and if more events are available.</returns>
		Task<(AccountEvent[], bool)> GetAccountEventsAsync(int MaxCount, DateTime From);

		/// <summary>
		/// Gets the current account balance.
		/// </summary>
		/// <returns>Current account balance.</returns>
		Task<Balance> GetBalanceAsync();

		/// <summary>
		/// Gets pending payments
		/// </summary>
		/// <returns>(Total amount, currency, items)</returns>
		Task<(decimal, string, PendingPayment[])> GetPendingPayments();

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="ToBareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <returns>Signed payment URI.</returns>
		Task<string> CreateFullPaymentUri(string ToBareJid, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays);

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
		Task<string> CreateFullPaymentUri(string ToBareJid, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays, string Message);

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <returns>Signed payment URI.</returns>
		Task<string> CreateFullPaymentUri(LegalIdentity To, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays);

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
		Task<string> CreateFullPaymentUri(LegalIdentity To, decimal Amount, decimal? AmountExtra, string Currency, int ValidNrDays, string PrivateMessage);

		/// <summary>
		/// Creates an incomplete PayMe-URI.
		/// </summary>
		/// <param name="BareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="AmountExtra">Any amount extra of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="Message">Message to be sent to recipient (not encrypted).</param>
		/// <returns>Incomplete PayMe-URI.</returns>
		string CreateIncompletePayMeUri(string BareJid, decimal? Amount, decimal? AmountExtra, string Currency, string Message);

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
		string CreateIncompletePayMeUri(LegalIdentity To, decimal? Amount, decimal? AmountExtra, string Currency, string PrivateMessage);

		/// <summary>
		/// Last reported balance
		/// </summary>
		Balance LastBalance { get; }

		/// <summary>
		/// Timepoint of last eDaler event.
		/// </summary>
		DateTime LastEDalerEvent { get; }

		/// <summary>
		/// Gets available service providers for buying eDaler.
		/// </summary>
		/// <returns>Available service providers.</returns>
		Task<IBuyEDalerServiceProvider[]> GetServiceProvidersForBuyingEDalerAsync();

		/// <summary>
		/// Initiates payment of eDaler using a service provider that is not based on a smart contract.
		/// </summary>
		/// <param name="ServiceId">Service ID</param>
		/// <param name="ServiceProvider">Service Provider</param>
		/// <param name="Amount">Amount</param>
		/// <param name="Currency">Currency</param>
		/// <returns>Transaction ID</returns>
		Task<PaymentTransaction> InitiatePayment(string ServiceId, string ServiceProvider, decimal Amount, string Currency);

		/// <summary>
		/// Registers an initiated payment as completed.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <param name="Amount">Amount</param>
		/// <param name="Currency">Currency</param>
		void PaymentCompleted(string TransactionId, decimal Amount, string Currency);

		/// <summary>
		/// Registers an initiated payment as failed.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <param name="Message">Error message.</param>
		void PaymentFailed(string TransactionId, string Message);

		#endregion

		#region Neuro-Features

		/// <summary>
		/// Reference to the Neuro-Features client implementing the Neuro-Features XMPP extension
		/// </summary>
		NeuroFeaturesClient NeuroFeaturesClient { get; }

		/// <summary>
		/// Event raised when a token has been removed from the wallet.
		/// </summary>
		event TokenEventHandler TokenRemoved;

		/// <summary>
		/// Event raised when a token has been added to the wallet.
		/// </summary>
		event TokenEventHandler TokenAdded;

		/// <summary>
		/// Event raised when variables have been updated in a state-machine.
		/// </summary>
		event VariablesUpdatedEventHandler VariablesUpdated;

		/// <summary>
		/// Event raised when a state-machine has received a new state.
		/// </summary>
		event NewStateEventHandler StateUpdated;

		/// <summary>
		/// Timepoint of last Neuro-Feature token event.
		/// </summary>
		DateTime LastTokenEvent { get; }

		/// <summary>
		/// Gets available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		Task<TokensEventArgs> GetTokens();

		/// <summary>
		/// Gets a section of available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		Task<TokensEventArgs> GetTokens(int Offset, int MaxCount);

		/// <summary>
		/// Gets references to available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		Task<string[]> GetTokenReferences();

		/// <summary>
		/// Gets references to a section of available tokens
		/// </summary>
		/// <returns>Response with tokens.</returns>
		Task<string[]> GetTokenReferences(int Offset, int MaxCount);

		/// <summary>
		/// Gets the value totals of tokens available in the wallet, grouped and ordered by currency.
		/// </summary>
		/// <returns>Response with tokens.</returns>
		Task<TokenTotalsEventArgs> GetTotals();

		/// <summary>
		/// Gets a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <returns>Token</returns>
		Task<Token> GetToken(string TokenId);

		/// <summary>
		/// Gets events relating to a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <returns>Token events.</returns>
		Task<TokenEvent[]> GetTokenEvents(string TokenId);

		/// <summary>
		/// Gets events relating to a specific token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="Offset">Offset </param>
		/// <param name="MaxCount">Maximum number of events to return.</param>
		/// <returns>Token events.</returns>
		Task<TokenEvent[]> GetTokenEvents(string TokenId, int Offset, int MaxCount);

		/// <summary>
		/// Adds a text note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="TextNote">Text Note</param>
		Task AddTextNote(string TokenId, string TextNote);

		/// <summary>
		/// Adds a text note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="TextNote">Text Note</param>
		/// <param name="Personal">If the text note contains personal information. (default=false).
		/// 
		/// Note: Personal notes are deleted when ownership of token is transferred.</param>
		Task AddTextNote(string TokenId, string TextNote, bool Personal);

		/// <summary>
		/// Adds a xml note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="XmlNote">Xml Note</param>
		Task AddXmlNote(string TokenId, string XmlNote);

		/// <summary>
		/// Adds a xml note on a token.
		/// </summary>
		/// <param name="TokenId">Token ID</param>
		/// <param name="XmlNote">Xml Note</param>
		/// <param name="Personal">If the xml note contains personal information. (default=false).
		/// 
		/// Note: Personal notes are deleted when ownership of token is transferred.</param>
		Task AddXmlNote(string TokenId, string XmlNote, bool Personal);

		/// <summary>
		/// Gets token creation attributes from the broker.
		/// </summary>
		/// <returns>Token creation attributes.</returns>
		Task<CreationAttributesEventArgs> GetCreationAttributes();

		#endregion
	}
}
