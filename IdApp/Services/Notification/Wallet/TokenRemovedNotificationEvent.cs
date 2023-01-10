using NeuroFeatures;
using System.Text;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a token that has been removed.
	/// </summary>
	public class TokenRemovedNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a token that has been removed.
		/// </summary>
		public TokenRemovedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a token that has been removed.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public TokenRemovedNotificationEvent(TokenEventArgs e)
			: base(e)
		{
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			StringBuilder sb = new();

			sb.Append(LocalizationResourceManager.Current["TokenRemoved2"]);
			sb.Append(": ");
			sb.Append(await base.GetDescription(ServiceReferences));

			return sb.ToString();
		}
	}
}
