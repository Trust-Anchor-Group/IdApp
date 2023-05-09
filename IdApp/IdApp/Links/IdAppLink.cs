using IdApp.Services;
using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Security.JWT;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Links
{
	/// <summary>
	/// Opens ID App links.
	/// </summary>
	public class IdAppLink : ILinkOpener
	{
		/// <summary>
		/// Opens ID App links.
		/// </summary>
		public IdAppLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.ToLower() == Constants.UriSchemes.UriSchemeTagIdApp ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public Task<bool> TryOpenLink(Uri Link)
		{
			ServiceReferences Services = new();
			string Token = Constants.UriSchemes.RemoveScheme(Link.OriginalString);
			JwtToken Parsed = Services.CryptoService.ParseAndValidateJwtToken(Token);
			if (Parsed is null)
				return Task.FromResult(false);

			if (!Parsed.TryGetClaim("cmd", out object Obj) || Obj is not string Command ||
				!Parsed.TryGetClaim(JwtClaims.ClientId, out Obj) || Obj is not string ClientId ||
				ClientId != Services.CryptoService.DeviceID ||
				!Parsed.TryGetClaim(JwtClaims.Issuer, out Obj) || Obj is not string Issuer ||
				Issuer != Services.CryptoService.DeviceID ||
				!Parsed.TryGetClaim(JwtClaims.Subject, out Obj) || Obj is not string Subject ||
				Subject != Services.XmppService.BareJid)
			{
				return Task.FromResult(false);
			}

			switch (Command)
			{
				case "bes":  // Buy eDaler Successful
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId ||
						!Parsed.TryGetClaim("amt", out object Amount) ||
						!Parsed.TryGetClaim("cur", out Obj) || Obj is not string Currency)
					{
						return Task.FromResult(false);
					}

					decimal AmountDec;

					try
					{
						AmountDec = Convert.ToDecimal(Amount);
					}
					catch (Exception)
					{
						return Task.FromResult(false);
					}

					Services.XmppService.BuyEDalerCompleted(TransactionId, AmountDec, Currency);
					return Task.FromResult(true);

				case "bef":  // Buy eDaler Failed
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId2)
						return Task.FromResult(false);

					Services.XmppService.BuyEDalerFailed(TransactionId2, LocalizationResourceManager.Current["PaymentFailed"]);
					return Task.FromResult(true);

				case "bec":  // Buy eDaler Cancelled
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId3)
						return Task.FromResult(false);

					Services.XmppService.BuyEDalerFailed(TransactionId3, LocalizationResourceManager.Current["PaymentCancelled"]);
					return Task.FromResult(true);

				case "ses":  // Sell eDaler Successful
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId4 ||
						!Parsed.TryGetClaim("amt", out Amount) ||
						!Parsed.TryGetClaim("cur", out Obj) || Obj is not string Currency4)
					{
						return Task.FromResult(false);
					}

					try
					{
						AmountDec = Convert.ToDecimal(Amount);
					}
					catch (Exception)
					{
						return Task.FromResult(false);
					}

					Services.XmppService.SellEDalerCompleted(TransactionId4, AmountDec, Currency4);
					return Task.FromResult(true);

				case "sef":  // Sell eDaler Failed
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId5)
						return Task.FromResult(false);

					Services.XmppService.SellEDalerFailed(TransactionId5, LocalizationResourceManager.Current["PaymentFailed"]);
					return Task.FromResult(true);

				case "sec":  // Sell eDaler Cancelled
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId6)
						return Task.FromResult(false);

					Services.XmppService.SellEDalerFailed(TransactionId6, LocalizationResourceManager.Current["PaymentCancelled"]);
					return Task.FromResult(true);

				default:
					return Task.FromResult(false);
			}
		}
	}
}
