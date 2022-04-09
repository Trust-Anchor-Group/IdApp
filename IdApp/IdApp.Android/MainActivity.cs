using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Firebase.Iid;
using Firebase.Messaging;
using IdApp.Android.Nfc;
using IdApp.Nfc;
using IdApp.Services.Nfc;
using IdApp.Services.Ocr;
using System;
using System.Collections.Generic;
using Tesseract.Droid;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace IdApp.Android
{
	[Activity(Label = "@string/app_name", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
	[IntentFilter(new string[] { NfcAdapter.ActionNdefDiscovered }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		private static NfcAdapter nfcAdapter = null;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate(savedInstanceState);

			this.Init(savedInstanceState);
		}

		private void Init(Bundle savedInstanceState)
		{
			this.Window.SetFlags(
				WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Secure,
				WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Secure);

			nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			ZXing.Net.Mobile.Forms.Android.Platform.Init();
			Rg.Plugins.Popup.Popup.Init(this);

			IOcrService OcrService = Types.InstantiateDefault<IOcrService>(false);
			OcrService.RegisterApi(new TesseractApi(Application, AssetsDeployment.OncePerVersion));

			int Result = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
			if (Result == ConnectionResult.Success)
			{
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
					NotificationChannel MessagesChannel = new("Messages", "Instant Messages", NotificationImportance.High)
					{
						Description = "Channel for incoming Instant Message notifications"
					};
					NotificationChannel ReviewChannel = new("Requests", "Review and Signature Requests", NotificationImportance.High)
					{
						Description = "Channel for incoming Contract or Identity Review Requests, or Signature Requests"
					};
					NotificationChannel SignatureChannel = new("Events", "Events", NotificationImportance.Default)
					{
						Description = "Channel for events relating to Legal Identities, Smart Contracts, Payments or tokens"
					};

					NotificationManager NotificationManager = (NotificationManager)GetSystemService(NotificationService);
					NotificationManager.CreateNotificationChannels(new List<NotificationChannel>()
					{
						MessagesChannel, ReviewChannel, SignatureChannel
					});
				}
			}
			else
			{
				string Msg = "Unable to access Google Play Services. Push notification is disabled.";

				if (GoogleApiAvailability.Instance.IsUserResolvableError(Result))
					Msg += " Error reported: " + GoogleApiAvailability.Instance.GetErrorString(Result);

				Waher.Events.Log.Error(Msg);
			}

			global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
			LoadApplication(new App());
		}

		public override async void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
		{
			try
			{
				base.OnCreate(savedInstanceState, persistentState);

				this.Init(savedInstanceState);

				string Url = Intent?.Data?.EncodedAuthority;
				await App.OpenUrl(Url);
			}
			catch (Exception ex)
			{
				Waher.Events.Log.Critical(ex);
			}
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
		{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		protected override void OnResume()
		{
			base.OnResume();

			if (!(nfcAdapter is null))
			{
				IntentFilter TagDetected = new(NfcAdapter.ActionTagDiscovered);
				IntentFilter NDefDetected = new(NfcAdapter.ActionNdefDiscovered);
				IntentFilter TechDetected = new(NfcAdapter.ActionTechDiscovered);
				IntentFilter[] Filters = new IntentFilter[] { TagDetected, NDefDetected, TechDetected };

				Intent Intent = new Intent(this, this.GetType()).AddFlags(ActivityFlags.SingleTop);

				PendingIntent PendingIntent = PendingIntent.GetActivity(this, 0, Intent, 0);
				nfcAdapter.EnableForegroundDispatch(this, PendingIntent, Filters, null);
			}
		}

		protected override async void OnNewIntent(Intent intent)
		{
			try
			{
				switch (intent.Action)
				{
					case NfcAdapter.ActionTagDiscovered:
					case NfcAdapter.ActionNdefDiscovered:
					case NfcAdapter.ActionTechDiscovered:
						Tag Tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
						if (!(Tag is null))
						{
							byte[] ID = Tag.GetId();
							string[] TechList = Tag.GetTechList();
							List<INfcInterface> Interfaces = new();

							foreach (string Tech in TechList)
							{
								switch (Tech)
								{
									case "android.nfc.tech.IsoDep":
										Interfaces.Add(new IsoDepInterface(Tag));
										break;

									case "android.nfc.tech.MifareClassic":
										Interfaces.Add(new MifareClassicInterface(Tag));
										break;

									case "android.nfc.tech.MifareUltralight":
										Interfaces.Add(new MifareUltralightInterface(Tag));
										break;

									case "android.nfc.tech.Ndef":
										Interfaces.Add(new NdefInterface(Tag));
										break;

									case "android.nfc.tech.NdefFormatable":
										Interfaces.Add(new NdefFormatableInterface(Tag));
										break;

									case "android.nfc.tech.NfcA":
										Interfaces.Add(new NfcAInterface(Tag));
										break;

									case "android.nfc.tech.NfcB":
										Interfaces.Add(new NfcBInterface(Tag));
										break;

									case "android.nfc.tech.NfcBarcode":
										Interfaces.Add(new NfcBarcodeInterface(Tag));
										break;

									case "android.nfc.tech.NfcF":
										Interfaces.Add(new NfcFInterface(Tag));
										break;

									case "android.nfc.tech.NfcV":
										Interfaces.Add(new NfcVInterface(Tag));
										break;
								}
							}

							INfcService Service = App.Instantiate<INfcService>();
							await Service.TagDetected(new NfcTag(ID, Interfaces.ToArray()));
						}
						break;
				}
			}
			catch (Exception)
			{
				// TODO: Handle read & connection errors.
			}
		}

		public override void OnBackPressed()
		{
			Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed);
		}
	}
}