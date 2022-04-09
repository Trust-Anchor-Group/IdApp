using Android.App;
using Android.Content;
using Firebase.Messaging;
using IdApp.Services.Push;
using System.Collections.Generic;
using Waher.Runtime.Inventory;

namespace IdApp.Android.Push
{
    [Service]
    [IntentFilter(new[] 
    { 
        "com.google.firebase.MESSAGING_EVENT",
        "com.google.firebase.INSTANCE_ID_EVENT"
    })]
    public class MessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage Message)
        {
            string Body = Message.GetNotification().Body;
            ShowNotification(Body, Message.Data);
        }

		public override void OnNewToken(string p0)
		{
            IPushNotificationService PushService = Types.Instantiate<IPushNotificationService>(true);
            PushService?.NewToken(PushMessagingService.Firebase, p0);
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
    }
}