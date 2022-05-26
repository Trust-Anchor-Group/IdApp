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
			string ChannelId = userInfo.ObjectForKey(new NSString("channelId"))?.ToString() ?? string.Empty;
			string Title = userInfo.ObjectForKey(new NSString("myTitle"))?.ToString() ?? string.Empty;
			string Body = userInfo.ObjectForKey(new NSString("myBody"))?.ToString() ?? string.Empty;
			string AttachmentIcon = null;

			switch (ChannelId)
			{
				case Constants.PushChannels.Messages:
					AttachmentIcon = "NotificationChatIcon";
					Body = GetChatNotificationBody(Body, userInfo);
					break;

				case Constants.PushChannels.Petitions:
					AttachmentIcon = "NotificationPetitionIcon";
					Body = GetPetitionNotificationBody(Body, userInfo);
					break;

				case Constants.PushChannels.Identities:
					AttachmentIcon = "NotificationIdentitieIcon";
					Body = GetIdentitieNotificationBody(Body, userInfo);
					break;

				case Constants.PushChannels.Contracts:
					AttachmentIcon = "NotificationContractIcon";
					Body = GetContractNotificationBody(Body, userInfo);
					break;

				case Constants.PushChannels.EDaler:
					AttachmentIcon = "NotificationEDalerIcon";
					Body = GetEDalerNotificationBody(Body, userInfo);
					break;

				case Constants.PushChannels.Tokens:
					AttachmentIcon = "NotificationTokenIcon";
					Body = GetTokenNotificationBody(Body, userInfo);
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

		private string GetChatNotificationBody(string messageBody, NSDictionary userInfo)
		{
			string isObject = userInfo.ObjectForKey(new NSString("isObject"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(isObject) && bool.Parse(isObject))
			{
				messageBody = "[Sent you an object]";
			}

			return messageBody;
		}

		private string GetPetitionNotificationBody(string messageBody, NSDictionary userInfo)
		{
			string FromJid = userInfo.ObjectForKey(new NSString("fromJid"))?.ToString() ?? string.Empty;
			string RosterName = userInfo.ObjectForKey(new NSString("rosterName"))?.ToString() ?? string.Empty;

			return (string.IsNullOrEmpty(RosterName) ? FromJid : RosterName) + ": " + messageBody;
		}

		private string GetIdentitieNotificationBody(string messageBody, NSDictionary userInfo)
		{
			string LegalId = userInfo.ObjectForKey(new NSString("legalId"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(LegalId))
			{
				messageBody += "\n(" + LegalId + ")";
			}

			return messageBody;
		}

		private string GetContractNotificationBody(string messageBody, NSDictionary userInfo)
		{
			string LegalId = userInfo.ObjectForKey(new NSString("legalId"))?.ToString() ?? string.Empty;
			string ContractId = userInfo.ObjectForKey(new NSString("contractId"))?.ToString() ?? string.Empty;
			string Role = userInfo.ObjectForKey(new NSString("role"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(Role))
			{
				messageBody += "\n" + Role;
			}

			if (!string.IsNullOrEmpty(ContractId))
			{
				messageBody += "\n(" + ContractId + ")";
			}

			if (!string.IsNullOrEmpty(LegalId))
			{
				messageBody += "\n(" + LegalId + ")";
			}

			return messageBody;
		}

		private string GetEDalerNotificationBody(string messageBody, NSDictionary userInfo)
		{
			string Amount = userInfo.ObjectForKey(new NSString("amount"))?.ToString() ?? string.Empty;
			string Currency = userInfo.ObjectForKey(new NSString("currency"))?.ToString() ?? string.Empty;
			string Timestamp = userInfo.ObjectForKey(new NSString("timestamp"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(Amount))
			{
				messageBody += "\n" + Amount;

				if (!string.IsNullOrEmpty(Currency))
				{
					messageBody += " " + Currency;
				}

				if (!string.IsNullOrEmpty(Timestamp))
				{
					messageBody += " (" + Timestamp + ")";
				}
			}

			return messageBody;
		}

		private string GetTokenNotificationBody(string messageBody, NSDictionary userInfo)
		{
			string Value = userInfo.ObjectForKey(new NSString("value"))?.ToString() ?? string.Empty;
			string Currency = userInfo.ObjectForKey(new NSString("currency"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(Value))
			{
				messageBody += "\n" + Value;

				if (!string.IsNullOrEmpty(Currency))
				{
					messageBody += " " + Currency;
				}
			}

			return messageBody;
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
