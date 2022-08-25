using NeuroFeatures;
using System.Text;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a token that has been added.
	/// </summary>
	public class TokenAddedNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a token that has been added.
		/// </summary>
		public TokenAddedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a token that has been added.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public TokenAddedNotificationEvent(TokenEventArgs e)
			: base(e)
		{
		}

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(ServiceReferences ServiceReferences)
		{
			StringBuilder sb = new();

			sb.Append(LocalizationResourceManager.Current["TokenAdded2"]);
			sb.Append(": ");
			sb.Append(await base.GetDescription(ServiceReferences));

			return sb.ToString();
		}
	}
}
