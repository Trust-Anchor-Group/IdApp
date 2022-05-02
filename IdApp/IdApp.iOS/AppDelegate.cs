using System;
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
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate,
        IUNUserNotificationCenterDelegate, IMessagingDelegate
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
            UNUserNotificationCenter.Current.Delegate = this;
            // For data message sent via FCM
            Messaging.SharedInstance.Delegate = this;

            UNAuthorizationOptions authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
            UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) => {
                var q = granted;
            });

            UIApplication.SharedApplication.RegisterForRemoteNotifications();
        }

        /*
        [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
        public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, System.Action<UIBackgroundFetchResult> completionHandler)
        {
            completionHandler(UIBackgroundFetchResult.NewData);
        }
        */

        [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
        public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            ProcessNotification(notification);

            completionHandler(UNNotificationPresentationOptions.Badge | UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Sound);
        }

        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, System.Action completionHandler)
        {
            if (response.IsDefaultAction)
            {
                ProcessNotification(response.Notification);
            }

            completionHandler();
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

        private void ProcessNotification(UNNotification notification)
        {
            /*
            string title = notification.Request.Content.Title;
            string message = notification.Request.Content.Body;

            DependencyService.Get<INotificationManager>().ReceiveNotification(title, message);
            */
        }

        private void RemoveAllNotifications()
        {
            UNUserNotificationCenter UNCenter = UNUserNotificationCenter.Current;
            UNCenter.RemoveAllDeliveredNotifications();
            UNCenter.RemoveAllPendingNotificationRequests();
        }


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

        public async Task StartLongRunningBackgroundTask()
        {
            var _backgroundTaskID = UIApplication.SharedApplication.BeginBackgroundTask(() => {
                /*
                // this is called if task times out
                if (_backgroundTaskID != 0)
                {
                    UIApplication.SharedApplication.EndBackgroundTask(_backgroundTaskID);
                    _backgroundTaskID = 0;
                }
                */
            });

            try
            {
                /*
                var restService = FreshTinyIoCContainer.Current.Resolve<IRestClientService>();
                var result = await restService.GetDummyResult();

                var messagingService = FreshTinyIoCContainer.Current.Resolve<IMessagingService>();
                messagingService.Publish(new GotDataMessage { DataString = $"{result.foo} at {DateTime.Now:G}" });
                */
            }

            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
            }

            UIApplication.SharedApplication.EndBackgroundTask(_backgroundTaskID);
        }
    }
}