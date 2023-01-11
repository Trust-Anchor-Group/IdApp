using Firebase.CloudMessaging;
using IdApp.DeviceSpecific;
using IdApp.Services.Push;
using System;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.XMPP.Push;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.iOS.Push.MessagingService))]
namespace IdApp.iOS.Push
{
	internal class MessagingService : IGetPushNotificationToken
	{

		/// <summary>
		/// Gets a Push Notification token for the device.
		/// </summary>
		/// <returns>Token, Service used, and type of client.</returns>
		public async Task<TokenInformation> GetToken()
		{
			string Token = string.Empty;

			try
			{
				Log.Warning("GetToken 1");
				Token = Messaging.SharedInstance.FcmToken ?? string.Empty;
				Log.Warning("GetToken 2", Token.ToString());
			}
			catch (Exception ex)
			{
				Log.Warning("GetToken 3", ex.ToString());
				Log.Critical(ex);
			}

			TokenInformation TokenInformation = new()
			{
				Token = Token,
				ClientType = ClientType.iOS,
				Service = PushMessagingService.Firebase
			};

			return await Task.FromResult(TokenInformation);
		}
	}
}
