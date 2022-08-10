﻿using EDaler;
using IdApp.Pages.Wallet;
using IdApp.Pages.Wallet.EDalerReceived;
using IdApp.Resx;
using System.Threading.Tasks;
using Waher.Persistence;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about an incoming chat message.
	/// </summary>
	public class BalanceNotificationEvent : WalletNotificationEvent
	{
		/// <summary>
		/// Contains information about an incoming chat message.
		/// </summary>
		public BalanceNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Amount
		/// </summary>
		public decimal Amount { get; set; }

		/// <summary>
		/// Amount reserved
		/// </summary>
		public decimal Reserved { get; set; }

		/// <summary>
		/// Currency
		/// </summary>
		public CaseInsensitiveString Currency { get; set; }

		/// <summary>
		/// Account event
		/// </summary>
		public AccountEvent Event { get; set; }

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetCategoryDescription(ServiceReferences ServiceReferences)
		{
			if (string.IsNullOrEmpty(this.Event?.Remote))
				return AppResources.BalanceUpdated;
			else
				return await ContactInfo.GetFriendlyName(this.Event.Remote, ServiceReferences);
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			if ((this.Event?.Change ?? 0) > 0)
			{
				Balance Balance = new(this.Timestamp, this.Amount, this.Reserved, this.Currency, this.Event);

				await ServiceReferences.NavigationService.GoToAsync(nameof(EDalerReceivedPage), new EDalerBalanceNavigationArgs(Balance));
			}
			else
				await ServiceReferences.NeuroWalletOrchestratorService.OpenWallet();
		}
	}
}
