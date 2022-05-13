using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Firebase.Messaging;
using IdApp.DeviceSpecific;
using IdApp.Services.Push;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
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
			RemoteMessage.Notification Notification = Message.GetNotification();
			string Body = Notification.Body;
			string Title = Notification.Title;
			string ChannelId = Notification.ChannelId;

			switch (ChannelId)
			{
				case "Messages":
					ShowMessageNotification(Title, Body, ChannelId, Message.Data);
					break;

				case "Petitions":
					ShowPetitionNotification(Title, Body, ChannelId, Message.Data);
					break;

				case "Identities":
					ShowIdentitiesNotification(Title, Body, ChannelId, Message.Data);
					break;

				case "Contracts":
					ShowContractsNotification(Title, Body, ChannelId, Message.Data);
					break;

				case "eDaler":
					ShowEDalerNotification(Title, Body, ChannelId, Message.Data);
					break;

				case "Tokens":

				default:
					Log.Debug("Push Notification Message received on unrecognized channel.", Notification.ChannelId,
						new KeyValuePair<string, object>("Body", Notification.Body),
						new KeyValuePair<string, object>("Icon", Notification.Icon),
						new KeyValuePair<string, object>("ImageUrl", Notification.ImageUrl.ToString()),
						new KeyValuePair<string, object>("Link", Notification.Link.ToString()),
						new KeyValuePair<string, object>("Sound", Notification.Sound),
						new KeyValuePair<string, object>("Title", Notification.Title));
					break;
			}
		}

		public void ShowMessageNotification(string Title, string MessageBody, string ChannelId, IDictionary<string, string> Data)
		{
			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);

			Notification.Builder notificationBuilder = new Notification.Builder(this, ChannelId)
				.SetSmallIcon(Resource.Drawable.notification_action_background)
				.SetContentTitle(Title)
				.SetContentText(MessageBody)
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowPetitionNotification(string Title, string MessageBody, string ChannelId, IDictionary<string, string> Data)
		{
			string FromJid = string.Empty;
			string RosterName = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "fromJid":
						FromJid = P.Value;
						break;

					case "rosterName":
						RosterName = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);

			Notification.Builder notificationBuilder = new Notification.Builder(this, ChannelId)
				.SetSmallIcon(Resource.Drawable.notification_action_background)
				.SetContentTitle(Title)
				.SetContentText((string.IsNullOrEmpty(RosterName) ? FromJid : RosterName) + ": " + MessageBody)
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowIdentitiesNotification(string Title, string MessageBody, string ChannelId, IDictionary<string, string> Data)
		{
			string LegalId = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "legalId":
						LegalId = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			StringBuilder Content = new();
			Content.Append(MessageBody);

			if (!string.IsNullOrEmpty(LegalId))
			{
				Content.AppendLine();
				Content.Append('(');
				Content.Append(LegalId);
				Content.Append(')');
			}

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);

			Notification.Builder notificationBuilder = new Notification.Builder(this, ChannelId)
				.SetSmallIcon(Resource.Drawable.notification_action_background)
				.SetContentTitle(Title)
				.SetContentText(Content.ToString())
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowContractsNotification(string Title, string MessageBody, string ChannelId, IDictionary<string, string> Data)
		{
			string LegalId = string.Empty;
			string ContractId = string.Empty;
			string Role = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "legalId":
						LegalId = P.Value;
						break;

					case "contractId":
						ContractId = P.Value;
						break;

					case "role":
						Role = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			StringBuilder Content = new();
			Content.Append(MessageBody);

			if (!string.IsNullOrEmpty(Role))
			{
				Content.AppendLine();
				Content.Append(Role);
			}

			if (!string.IsNullOrEmpty(ContractId))
			{
				Content.AppendLine();
				Content.Append('(');
				Content.Append(ContractId);
				Content.Append(')');
			}

			if (!string.IsNullOrEmpty(LegalId))
			{
				Content.AppendLine();
				Content.Append('(');
				Content.Append(LegalId);
				Content.Append(')');
			}

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);

			Notification.Builder notificationBuilder = new Notification.Builder(this, ChannelId)
				.SetSmallIcon(Resource.Drawable.notification_action_background)
				.SetContentTitle(Title)
				.SetContentText(Content.ToString())
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowEDalerNotification(string Title, string MessageBody, string ChannelId, IDictionary<string, string> Data)
		{
			string Amount = string.Empty;
			string Currency = string.Empty;
			string Timestamp = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "amount":
						Amount = P.Value;
						break;

					case "currency":
						Currency = P.Value;
						break;

					case "timestamp":
						Timestamp = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			StringBuilder Content = new();
			Content.Append(MessageBody);

			if (!string.IsNullOrEmpty(Amount))
			{
				Content.AppendLine();
				Content.Append(Amount);


				if (!string.IsNullOrEmpty(Currency))
				{
					Content.Append(' ');
					Content.Append(Currency);
				}

				if (!string.IsNullOrEmpty(Timestamp))
				{
					Content.Append(" (");
					Content.Append(Timestamp);
					Content.Append(')');
				}
			}

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);

			Notification.Builder notificationBuilder = new Notification.Builder(this, ChannelId)
				.SetSmallIcon(Resource.Drawable.notification_action_background)
				.SetContentTitle(Title)
				.SetContentText(Content.ToString())
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowTokenNotification(string Title, string MessageBody, string ChannelId, IDictionary<string, string> Data)
		{
			string Value = string.Empty;
			string Currency = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "value":
						Value = P.Value;
						break;

					case "currency":
						Currency = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			StringBuilder Content = new();
			Content.Append(MessageBody);

			if (!string.IsNullOrEmpty(Value))
			{
				Content.AppendLine();
				Content.Append(Value);


				if (!string.IsNullOrEmpty(Currency))
				{
					Content.Append(' ');
					Content.Append(Currency);
				}
			}

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);

			Notification.Builder notificationBuilder = new Notification.Builder(this, ChannelId)
				.SetSmallIcon(Resource.Drawable.notification_action_background)
				.SetContentTitle(Title)
				.SetContentText(Content.ToString())
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public override void OnNewToken(string p0)
		{
			try
			{
				IPushNotificationService PushService = Types.Instantiate<IPushNotificationService>(true);
				PushService?.NewToken(new TokenInformation()
				{
					Service = Waher.Networking.XMPP.Push.PushMessagingService.Firebase,
					Token = p0,
					ClientType = ClientType.Android
				});
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
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