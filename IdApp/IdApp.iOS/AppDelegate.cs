using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGraphics;
using Firebase.CloudMessaging;
using Foundation;
using IdApp.Helpers;
using IdApp.Services.Ocr;
using IdApp.Services.Push;
using Tesseract.iOS;
using UIKit;
using UserNotifications;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate, IMessagingDelegate
	{
		NSObject OnKeyboardShowObserver;
		NSObject OnKeyboardHideObserver;

		//
		// This method is invoked when the application has loaded and is ready to run. In this
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			Firebase.Core.App.Configure();
			Rg.Plugins.Popup.Popup.Init();
			ZXing.Net.Mobile.Forms.iOS.Platform.Init();

			global::Xamarin.Forms.Forms.Init();
			LoadApplication(new App());

			IOcrService OcrService = Types.InstantiateDefault<IOcrService>(false);
			OcrService.RegisterApi(new TesseractApi());

			RegisterKeyBoardObserver();
			RegisterRemoteNotifications();

			return base.FinishedLaunching(app, options);
		}

		public override void WillTerminate(UIApplication application)
		{
			if (OnKeyboardShowObserver == null)
			{
				OnKeyboardShowObserver.Dispose();
				OnKeyboardShowObserver = null;
			}

			if (OnKeyboardHideObserver == null)
			{
				OnKeyboardHideObserver.Dispose();
				OnKeyboardHideObserver = null;
			}
		}

		public override void WillEnterForeground(UIApplication uiApplication)
		{
			base.WillEnterForeground(uiApplication);

			RemoveAllNotifications();
		}

		/// <summary>
		/// Method is called when an URL with a registered schema is being opened.
		/// </summary>
		/// <param name="app">Application</param>
		/// <param name="url">URL</param>
		/// <param name="options">Options</param>
		/// <returns>If URL is handled.</returns>
		public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
		{
			return App.OpenUrl(url.AbsoluteString).Result;
		}

		private void RegisterKeyBoardObserver()
		{
			if (OnKeyboardShowObserver == null)
			{
				OnKeyboardShowObserver = UIKeyboard.Notifications.ObserveWillShow((object sender, UIKeyboardEventArgs args) =>
				{
					NSValue result = (NSValue)args.Notification.UserInfo.ObjectForKey(new NSString(UIKeyboard.FrameEndUserInfoKey));
					CGSize keyboardSize = result.RectangleFValue.Size;
					MessagingCenter.Send<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears, new KeyboardAppearEventArgs { KeyboardSize = (float)keyboardSize.Height });
				});
			}

			if (OnKeyboardHideObserver == null)
			{
				OnKeyboardHideObserver = UIKeyboard.Notifications.ObserveWillHide((object sender, UIKeyboardEventArgs args) =>
				{
					MessagingCenter.Send<object>(this, Constants.MessagingCenter.KeyboardDisappears);
				});
			}
		}

		private void RegisterRemoteNotifications()
		{
			RemoveAllNotifications();

			// For display notification sent via APNS
			UNUserNotificationCenter.Current.Delegate = new UserNotificationCenterDelegate();
			// For data message sent via FCM
			Messaging.SharedInstance.Delegate = this;

			UNAuthorizationOptions authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
			UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) =>
			{
				bool q = granted;
			});

			UIApplication.SharedApplication.RegisterForRemoteNotifications();
		}

		[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
		public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		{
			string MessageId = userInfo.ObjectForKey(new NSString("gcm.message_id")).ToString();
			string ChannelId = userInfo.ObjectForKey(new NSString("iconType")).ToString();
			string Title = userInfo.ObjectForKey(new NSString("myTitle")).ToString();
			string Body = string.Empty;
			string AttachmentIcon = null;

			switch (ChannelId)
			{
				case Constants.PushChannels.Messages:
					AttachmentIcon = "NotificationChatIcon";
					Body = GetChatNotificationBody(userInfo);
					break;

				case Constants.PushChannels.Petitions:
					AttachmentIcon = "NotificationPetitionIcon";
					Body = GetPetitionNotificationBody(userInfo);
					break;

				case Constants.PushChannels.Identities:
					AttachmentIcon = "NotificationIdentitieIcon";
					Body = GetIdentitieNotificationBody(userInfo);
					break;

				case Constants.PushChannels.Contracts:
					AttachmentIcon = "NotificationContractIcon";
					Body = GetContractNotificationBody(userInfo);
					break;

				case Constants.PushChannels.EDaler:
					AttachmentIcon = "NotificationEDalerIcon";
					Body = GetEDalerNotificationBody(userInfo);
					break;

				case Constants.PushChannels.Tokens:
					AttachmentIcon = "NotificationTokenIcon";
					Body = GetTokenNotificationBody(userInfo);
					break;

				default:
					break;
			}

			// Create request
			UNMutableNotificationContent content = new()
			{
				Title = Title,
				Body = Body,
			};

			if (AttachmentIcon is not null)
			{
				NSError err;
				UNNotificationAttachmentOptions options = new();
				NSUrl AttachmentUrl = NSBundle.MainBundle.GetUrlForResource(AttachmentIcon, "png");

				if (AttachmentUrl is not null)
				{
					UNNotificationAttachment attachment = UNNotificationAttachment.FromIdentifier("image" + MessageId, AttachmentUrl, options, out err);

					if (attachment is not null)
					{
						content.Attachments = new UNNotificationAttachment[] { attachment };
					}
				}
			}

			UNNotificationRequest request = UNNotificationRequest.FromIdentifier(MessageId, content, null);
			UNUserNotificationCenter.Current.AddNotificationRequest(request, null);

			completionHandler(UIBackgroundFetchResult.NewData);
		}

		private string GetChatNotificationBody(NSDictionary userInfo)
		{
			string Body = userInfo.ObjectForKey(new NSString("myBody")).ToString();

			return Body;
		}

		private string GetPetitionNotificationBody(NSDictionary userInfo)
		{
			string Body = userInfo.ObjectForKey(new NSString("myBody")).ToString();
			string FromJid = userInfo.ObjectForKey(new NSString("fromJid")).ToString();
			string RosterName = userInfo.ObjectForKey(new NSString("rosterName")).ToString();

			Body = (string.IsNullOrEmpty(RosterName) ? FromJid : RosterName) + ": " + Body;
			return Body;
		}

		private string GetIdentitieNotificationBody(NSDictionary userInfo)
		{
			string Body = userInfo.ObjectForKey(new NSString("myBody")).ToString();
			string LegalId = userInfo.ObjectForKey(new NSString("legalId")).ToString();

			if (!string.IsNullOrEmpty(LegalId))
			{
				Body = "\n(" + LegalId + ")";
			}

			return Body;
		}

		private string GetContractNotificationBody(NSDictionary userInfo)
		{
			string Body = userInfo.ObjectForKey(new NSString("myBody")).ToString();
			string LegalId = userInfo.ObjectForKey(new NSString("legalId")).ToString();
			string ContractId = userInfo.ObjectForKey(new NSString("contractId")).ToString();
			string Role = userInfo.ObjectForKey(new NSString("role")).ToString();

			if (!string.IsNullOrEmpty(Role))
			{
				Body += "\n" + Role;
			}

			if (!string.IsNullOrEmpty(ContractId))
			{
				Body += "\n(" + ContractId + ")";
			}

			if (!string.IsNullOrEmpty(LegalId))
			{
				Body += "\n(" + LegalId + ")";
			}

			return Body;
		}

		private string GetEDalerNotificationBody(NSDictionary userInfo)
		{
			string Body = userInfo.ObjectForKey(new NSString("myBody")).ToString();
			string Amount = userInfo.ObjectForKey(new NSString("amount")).ToString();
			string Currency = userInfo.ObjectForKey(new NSString("currency")).ToString();
			string Timestamp = userInfo.ObjectForKey(new NSString("timestamp")).ToString();

			if (!string.IsNullOrEmpty(Amount))
			{
				Body += "\n" + Amount;

				if (!string.IsNullOrEmpty(Currency))
				{
					Body += " " + Currency;
				}

				if (!string.IsNullOrEmpty(Timestamp))
				{
					Body += " (" + Timestamp + ")";
				}
			}

			return Body;
		}

		private string GetTokenNotificationBody(NSDictionary userInfo)
		{
			string Body = userInfo.ObjectForKey(new NSString("myBody")).ToString();
			string Value = userInfo.ObjectForKey(new NSString("value")).ToString();
			string Currency = userInfo.ObjectForKey(new NSString("currency")).ToString();

			if (!string.IsNullOrEmpty(Value))
			{
				Body += "\n" + Value;

				if (!string.IsNullOrEmpty(Currency))
				{
					Body += " " + Currency;
				}
			}

			return Body;
		}

		[Export("messaging:didReceiveRegistrationToken:")]
        public void DidReceiveRegistrationToken(Messaging messaging, string fcmToken)
        {
            try
            {
                IPushNotificationService PushService = Types.Instantiate<IPushNotificationService>(true);
                PushService?.NewToken(new TokenInformation()
                {
                    Service = Waher.Networking.XMPP.Push.PushMessagingService.Firebase,
                    Token = fcmToken,
                    ClientType = ClientType.iOS
                });
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
            }
        }

        private void RemoveAllNotifications()
        {
            UNUserNotificationCenter UNCenter = UNUserNotificationCenter.Current;
            UNCenter.RemoveAllDeliveredNotifications();
            UNCenter.RemoveAllPendingNotificationRequests();
        }

        /*
        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
        {
            try
            {
                completionHandler(UIBackgroundFetchResult.NewData);
            }
            catch (Exception)
            {
                completionHandler(UIBackgroundFetchResult.NoData);
            }
        }
        */
        /*
        public async Task StartLongRunningBackgroundTask()
        {
            var _backgroundTaskID = UIApplication.SharedApplication.BeginBackgroundTask(() => {
                // this is called if task times out
                //if (_backgroundTaskID != 0)
                //{
                //    UIApplication.SharedApplication.EndBackgroundTask(_backgroundTaskID);
                //    _backgroundTaskID = 0;
                //}
            });

            try
            {
                //var restService = FreshTinyIoCContainer.Current.Resolve<IRestClientService>();
                //var result = await restService.GetDummyResult();
                //var messagingService = FreshTinyIoCContainer.Current.Resolve<IMessagingService>();
                //messagingService.Publish(new GotDataMessage { DataString = $"{result.foo} at {DateTime.Now:G}" });
            }

            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
            }

            UIApplication.SharedApplication.EndBackgroundTask(_backgroundTaskID);
        }
        */
    }

    public class UserNotificationCenterDelegate : UNUserNotificationCenterDelegate
    {
        public UserNotificationCenterDelegate()
        {
        }

        [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            ProcessNotification(notification);

            UNNotificationPresentationOptions options = UNNotificationPresentationOptions.None;
            completionHandler(options);
        }

        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            ProcessNotification(response.Notification);
        }

        private void ProcessNotification(UNNotification notification)
        {
            Console.WriteLine(notification.Request.Content.ToString());
            /*
            string title = notification.Request.Content.Title;
            string message = notification.Request.Content.Body;

            DependencyService.Get<INotificationManager>().ReceiveNotification(title, message);
            */
        }
    }
}
