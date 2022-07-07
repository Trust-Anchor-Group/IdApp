using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Views;
using IdApp.Android.Nfc;
using IdApp.Nfc;
using IdApp.Services.Nfc;
using IdApp.Services.Ocr;
using System;
using System.Collections.Generic;
using Tesseract.Droid;
using Waher.Runtime.Inventory;

namespace IdApp.Android
{
	[Activity(Label = "@string/app_name", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.Locale, ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
	[IntentFilter(new string[] { NfcAdapter.ActionNdefDiscovered }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		private static NfcAdapter nfcAdapter = null;

		protected override void OnCreate(Bundle SavedInstanceState)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate(SavedInstanceState);

			this.Init(SavedInstanceState);
		}

		private void Init(Bundle SavedInstanceState)
		{
			this.Window.SetFlags(
				WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Secure,
				WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Secure);

			nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

			Xamarin.Essentials.Platform.Init(this, SavedInstanceState);
			ZXing.Net.Mobile.Forms.Android.Platform.Init();
			Rg.Plugins.Popup.Popup.Init(this);
			Helpers.Svg.SvgImage.Init(this);
			FFImageLoading.Forms.Platform.CachedImageRenderer.Init(enableFastRenderer: true);

			IOcrService OcrService = Types.InstantiateDefault<IOcrService>(false);
			OcrService.RegisterApi(new TesseractApi(this.Application, AssetsDeployment.OncePerVersion));

			int Result = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
			if (Result == ConnectionResult.Success)
			{
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
					NotificationChannel MessagesChannel = new("Messages", "Instant Messages", NotificationImportance.High)
					{
						Description = "Channel for incoming Instant Message notifications"
					};

					NotificationChannel PetitionsChannel = new("Petitions", "Petitions sent by other users", NotificationImportance.High)
					{
						Description = "Channel for incoming Contract or Identity Peititions, such as Review or Signature Requests"
					};

					NotificationChannel IdentitiesChannel = new("Identities", "Identity events", NotificationImportance.High)
					{
						Description = "Channel for events relating to the digital identity"
					};

					NotificationChannel ContractsChannel = new("Contracts", "Contract events", NotificationImportance.High)
					{
						Description = "Channel for events relating to smart contracts"
					};

					NotificationChannel EDalerChannel = new("eDaler", "eDaler events", NotificationImportance.High)
					{
						Description = "Channel for events relating to the eDaler wallet balance"
					};

					NotificationChannel TokensChannel = new("Tokens", "Token events", NotificationImportance.High)
					{
						Description = "Channel for events relating to Neuro-Feature tokens"
					};

					NotificationManager NotificationManager = (NotificationManager)this.GetSystemService(NotificationService);
					NotificationManager.CreateNotificationChannels(new List<NotificationChannel>()
					{
						MessagesChannel, PetitionsChannel, IdentitiesChannel, ContractsChannel, EDalerChannel, TokensChannel
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

			global::Xamarin.Forms.Forms.Init(this, SavedInstanceState);

			// This must be called after Xamarin.Forms.Forms.Init.
			FFImageLoading.Forms.Platform.CachedImageRenderer.InitImageViewHandler();

			FFImageLoading.Config.Configuration Configuration = FFImageLoading.Config.Configuration.Default;
			Configuration.DiskCacheDuration = TimeSpan.FromDays(1);
			FFImageLoading.ImageService.Instance.Initialize(Configuration);

			// Uncomment this to debug loading images from neuron (ensures that they are not loaded from cache).
			// FFImageLoading.ImageService.Instance.InvalidateCacheAsync(FFImageLoading.Cache.CacheType.Disk);

			this.LoadApplication(new App());
		}

		public override async void OnCreate(Bundle SavedInstanceState, PersistableBundle PersistentState)
		{
			try
			{
				base.OnCreate(SavedInstanceState, PersistentState);

				this.Init(SavedInstanceState);

				string Url = this.Intent?.Data?.EncodedAuthority;
				await App.OpenUrl(Url);
			}
			catch (Exception ex)
			{
				Waher.Events.Log.Critical(ex);
			}
		}

		public override void OnRequestPermissionsResult(int RequestCode, string[] Permissions, [GeneratedEnum] Permission[] GrantResults)
		{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(RequestCode, Permissions, GrantResults);
			base.OnRequestPermissionsResult(RequestCode, Permissions, GrantResults);
		}

		protected override void OnResume()
		{
			base.OnResume();

			if (nfcAdapter is not null)
			{
				IntentFilter TagDetected = new(NfcAdapter.ActionTagDiscovered);
				IntentFilter NDefDetected = new(NfcAdapter.ActionNdefDiscovered);
				IntentFilter TechDetected = new(NfcAdapter.ActionTechDiscovered);
				IntentFilter[] Filters = new IntentFilter[] { TagDetected, NDefDetected, TechDetected };

				Intent Intent = new Intent(this, this.GetType()).AddFlags(ActivityFlags.SingleTop);

				PendingIntent PendingIntent = PendingIntent.GetActivity(this, 0, Intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
				nfcAdapter.EnableForegroundDispatch(this, PendingIntent, Filters, null);
			}

			this.RemoveAllNotifications();
		}

		protected override async void OnNewIntent(Intent Intent)
		{
			try
			{
				switch (Intent.Action)
				{
					case NfcAdapter.ActionTagDiscovered:
					case NfcAdapter.ActionNdefDiscovered:
					case NfcAdapter.ActionTechDiscovered:
						Tag Tag = Intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;

						if (Tag is not null)
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
			catch (Exception ex)
			{
				Waher.Events.Log.Critical(ex);
				// TODO: Handle read & connection errors.
			}
		}

		public override void OnBackPressed()
		{
			Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed);
		}

		private void RemoveAllNotifications()
		{
			NotificationManager Manager = (NotificationManager)this.GetSystemService(Context.NotificationService);
			Manager.CancelAll();
		}
	}
}
