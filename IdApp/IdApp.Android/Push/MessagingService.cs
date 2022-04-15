using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Firebase.Messaging;
using IdApp.DeviceSpecific;
using IdApp.Services.Push;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.Push.MessagingService))]
namespace IdApp.Android.Push
{
	[Service]
	[IntentFilter(new[]
	{
		"com.google.firebase.MESSAGING_EVENT",
		"com.google.firebase.INSTANCE_ID_EVENT"
	})]
	public class MessagingService : FirebaseMessagingService, IGetPushNotificationToken
	{
		public override void OnMessageReceived(RemoteMessage Message)
		{
			string Body = Message.GetNotification().Body;
			ShowNotification(Body, Message.Data);
		}

		public override void OnNewToken(string p0)
		{
			IPushNotificationService PushService = Types.Instantiate<IPushNotificationService>(true);
			PushService?.NewToken(new TokenInformation()
			{
				Service = Waher.Networking.XMPP.Push.PushMessagingService.Firebase,
				Token = p0,
				ClientType = ClientType.Android
			});
		}

		public void ShowNotification(string MessageBody, IDictionary<string, string> Data)
		{
			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);

			Notification.Builder notificationBuilder = new Notification.Builder(this, "Messages")
				.SetSmallIcon(Resource.Drawable.notification_action_background)
				.SetContentTitle("Message Received")
				.SetContentText(MessageBody)
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		/// <summary>
		/// Gets a Push Notification token for the device.
		/// </summary>
		/// <returns>Token, Service used, and type of client.</returns>
		public async Task<TokenInformation> GetToken()
		{
			Java.Lang.Object Token = await FirebaseMessaging.Instance.GetToken().AsAsync<Java.Lang.Object>();

			return new TokenInformation()
			{
				Token = Token.ToString(),
				ClientType = ClientType.Android,
				Service = Waher.Networking.XMPP.Push.PushMessagingService.Firebase
			};
		}

	}
}