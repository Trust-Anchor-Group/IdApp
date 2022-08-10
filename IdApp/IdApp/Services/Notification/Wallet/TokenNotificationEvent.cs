using System.Threading.Tasks;
using NeuroFeatures;
using IdApp.Pages.Wallet.TokenDetails;
using IdApp.Pages.Wallet.MyWallet.ObjectModels;
using Waher.Persistence.Attributes;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Abstract base class for token notification events.
	/// </summary>
	public abstract class TokenNotificationEvent : WalletNotificationEvent
	{
		/// <summary>
		/// Abstract base class for token notification events.
		/// </summary>
		public TokenNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Token ID
		/// </summary>
		public string TokenId { get; set; }

		/// <summary>
		/// Category of token
		/// </summary>
		public string TokenCategory { get; set; }

		/// <summary>
		/// Friendly Name of token
		/// </summary>
		public string FriendlyName { get; set; }

		/// <summary>
		/// Loaded token
		/// </summary>
		[IgnoreMember]
		internal Token Token { get; set; }

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override Task<string> GetCategoryDescription(ServiceReferences ServiceReferences)
		{
			if (!string.IsNullOrEmpty(this.FriendlyName))
				return Task.FromResult(this.FriendlyName);
			else if (!string.IsNullOrEmpty(this.TokenCategory))
				return Task.FromResult(this.TokenCategory);
			else
				return Task.FromResult(this.TokenId);
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			if (this.Token is null)
				this.Token = await ServiceReferences.XmppService.Wallet.GetToken(this.TokenId);

			await ServiceReferences.NavigationService.GoToAsync(nameof(TokenDetailsPage),
				new TokenDetailsNavigationArgs(new TokenItem(this.Token, ServiceReferences)) { ReturnCounter = 1 });
		}
	}
}
