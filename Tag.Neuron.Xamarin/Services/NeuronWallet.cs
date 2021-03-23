﻿using System;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace Tag.Neuron.Xamarin.Services
{
	[Singleton]
	internal sealed class NeuronWallet : INeuronWallet
	{
		private readonly IInternalNeuronService neuronService;
		private readonly ILogService logService;
		private EDalerClient eDalerClient;

		internal NeuronWallet(IInternalNeuronService neuronService, ILogService logService)
		{
			this.neuronService = neuronService;
			this.logService = logService;
		}

		public EDalerClient EDalerClient
		{
			get
			{
				if (this.eDalerClient is null)
				{
					this.eDalerClient = this.neuronService.CreateEDalerClient();
					this.eDalerClient.BalanceUpdated += EDalerClient_BalanceUpdated;
				}

				return this.eDalerClient;
			}
		}

		private async Task EDalerClient_BalanceUpdated(object Sender, BalanceEventArgs e)
		{
			BalanceEventHandler h = this.BalanceUpdated;
			if (!(h is null))
			{
				try
				{
					await h(this, e);
				}
				catch (Exception ex)
				{
					this.logService.LogException(ex);
				}
			}
		}

		public event BalanceEventHandler BalanceUpdated;

		internal void DestroyClient()
		{
			this.eDalerClient?.Dispose();
			this.eDalerClient = null;
		}

		/// <summary>
		/// Tries to parse an eDaler URI.
		/// </summary>
		/// <param name="Uri">URI string.</param>
		/// <param name="Parsed">Parsed eDaler URI, if successful.</param>
		/// <returns>If URI string could be parsed.</returns>
		public bool TryParseEDalerUri(string Uri, out EDalerUri Parsed)
		{
			return EDalerUri.TryParse(Uri, out Parsed);
		}

		/// <summary>
		/// Tries to decrypt an encrypted private message.
		/// </summary>
		/// <param name="EncryptedMessage">Encrypted message.</param>
		/// <param name="PublicKey">Public key used.</param>
		/// <param name="RemoteEndpoint">Remote endpoint</param>
		/// <returns>Decrypted string, if successful, or null, if not.</returns>
		public async Task<string> TryDecryptMessage(byte[] EncryptedMessage, byte[] PublicKey, string RemoteEndpoint)
		{
			try
			{
				// TODO: Replace with method overload from EDaler v1.0.3:
				if (!string.IsNullOrEmpty(RemoteEndpoint) && !(PublicKey is null))
				{
					string RemotePublicKey = await RuntimeSettings.GetAsync("EDaler.Remote.Key." + RemoteEndpoint, string.Empty);

					if (!string.IsNullOrEmpty(RemotePublicKey))
						PublicKey = Convert.FromBase64String(RemotePublicKey);
				}

				return await this.eDalerClient.DecryptMessage(EncryptedMessage, PublicKey);
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
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullPaymentUri(string ToBareJid, decimal Amount, string Currency, int ValidNrDays)
		{
			return this.EDalerClient.CreateFullPaymentUri(ToBareJid, Amount, Currency, ValidNrDays);
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="ToBareJid">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <param name="Message">Unencrypted message to send to recipient.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullPaymentUri(string ToBareJid, decimal Amount, string Currency, int ValidNrDays, string Message)
		{
			return this.EDalerClient.CreateFullPaymentUri(ToBareJid, Amount, Currency, ValidNrDays);	// TODO: , Message);
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullPaymentUri(LegalIdentity To, decimal Amount, string Currency, int ValidNrDays)
		{
			return this.EDalerClient.CreateFullPaymentUri(To, Amount, Currency, ValidNrDays, string.Empty);	// TODO: );
		}

		/// <summary>
		/// Creates a full payment URI.
		/// </summary>
		/// <param name="To">To whom the payment is to be made.</param>
		/// <param name="Amount">Amount of eDaler to pay.</param>
		/// <param name="Currency">Currency to pay.</param>
		/// <param name="ValidNrDays">For how many days the URI should be valid.</param>
		/// <param name="PrivateMessage">Message to be sent to recipient. Message will be end-to-end encrypted.</param>
		/// <returns>Signed payment URI.</returns>
		public Task<string> CreateFullPaymentUri(LegalIdentity To, decimal Amount, string Currency, int ValidNrDays, string PrivateMessage)
		{
			return this.EDalerClient.CreateFullPaymentUri(To, Amount, Currency, ValidNrDays, PrivateMessage);
		}

	}
}