using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Views;
using System;
using Waher.Events;

namespace IdApp.Android
{
    [Activity(Label = "@string/app_name", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
    [IntentFilter(new[] { NfcAdapter.ActionNdefDiscovered }, Categories = new[] { Intent.CategoryDefault })]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            this.Window.SetFlags(
                WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Secure,
                WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Secure);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            ZXing.Net.Mobile.Forms.Android.Platform.Init();
            Rg.Plugins.Popup.Popup.Init(this);
            NearFieldCommunication.OnCreated(this);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

		public override async void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
		{
            try
            {
                base.OnCreate(savedInstanceState, persistentState);

                string Url = Intent?.Data?.EncodedAuthority;

                await App.OpenUrl(Url);
            }
            catch (Exception ex)
			{
                Log.Critical(ex);
			}
        }

		protected override void OnResume()
		{
			base.OnResume();
            NearFieldCommunication.OnResumed(this);
        }

        protected override void OnNewIntent(Intent intent)
		{
			base.OnNewIntent(intent);
            NearFieldCommunication.OnNewIntent(intent);
		}

		public override void OnBackPressed()
		{
            Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed);
        }
	}
}