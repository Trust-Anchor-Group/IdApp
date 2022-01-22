using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Android.Runtime;
using Android.Views;
using System;
using Waher.Events;

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
				Log.Critical(ex);
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
				IntentFilter TagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
				IntentFilter NDefDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
				IntentFilter TechDetected = new IntentFilter(NfcAdapter.ActionTechDiscovered);
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

							foreach (string Tech in TechList)
							{
								switch (Tech)
								{
									case "android.nfc.tech.IsoDep":
										IsoDep IsoDep = IsoDep.Get(Tag);
										// TODO: Embed in IsoDep class
										break;

									case "android.nfc.tech.MifareClassic":
										using (MifareClassic MifareClassic = MifareClassic.Get(Tag))
										{
											await MifareClassic.ConnectAsync();
											try
											{
												MifareClassicType Type = MifareClassic.Type;
												int BlockCount = MifareClassic.BlockCount;
												int SectorCount = MifareClassic.SectorCount;
												int TotalBytes = BlockCount << 4;
												byte[] Data = new byte[TotalBytes];
												int BlockIndex = 0;

												while (BlockIndex < BlockCount)
												{
													byte[] Block = await MifareClassic.ReadBlockAsync(BlockIndex++);
													Array.Copy(Block, 0, Data, BlockIndex << 4, 16);
												}

												// TODO: Embed in MifareClassic class
											}
											finally
											{
												MifareClassic.Close();
											}
										}
										break;

									case "android.nfc.tech.MifareUltralight":
										using (MifareUltralight MifareUltralight = MifareUltralight.Get(Tag))
										{
											await MifareUltralight.ConnectAsync();
											try
											{
												MifareUltralightType Type = MifareUltralight.Type;
												int TotalBytes;

												switch (Type)
												{
													case MifareUltralightType.Ultralight:
													case MifareUltralightType.Unknown:
													default:
														TotalBytes = 64;
														break;

													case MifareUltralightType.UltralightC:
														TotalBytes = 192;
														break;
												}

												int PageSize = MifareUltralight.PageSize;
												int NrPages = TotalBytes / PageSize;
												byte[] Data = new byte[TotalBytes];
												int Offset = 0;

												while (Offset < TotalBytes)
												{
													byte[] Pages = await MifareUltralight.ReadPagesAsync(Offset / PageSize);
													int i = Math.Min(Pages.Length, TotalBytes - Offset);
													if (i <= 0)
														throw new Exception("Unexpected end of data.");

													Array.Copy(Pages, 0, Data, Offset, i);
													Offset += i;
												}

												// TODO: Embed in MifareUltralight class
											}
											finally
											{
												MifareUltralight.Close();
											}
										}
										break;

									case "android.nfc.tech.Ndef":
										using (Ndef Ndef = Ndef.Get(Tag))
										{
											await Ndef.ConnectAsync();
											try
											{
												bool CanMakeReadOnly = Ndef.CanMakeReadOnly();
												bool IsWritable = Ndef.IsWritable;
												NdefMessage Message = Ndef.NdefMessage;
												NdefRecord[] Records = Message.GetRecords();

												// TODO: Embed in Ndef class
											}
											finally
											{
												Ndef.Close();
											}
										}
										break;

									case "android.nfc.tech.NdefFormatable":
										NdefFormatable NdefFormatable = NdefFormatable.Get(Tag);
										// TODO: Embed in NdefFormatable class
										break;

									case "android.nfc.tech.NfcA":
										using (NfcA NfcA = NfcA.Get(Tag))
										{
											await NfcA.ConnectAsync();
											try
											{
												byte[] Data = NfcA.GetAtqa();
												short Sak = NfcA.Sak;

												// TODO: Embed in NfcA class
											}
											finally
											{
												NfcA.Close();
											}
										}
										break;

									case "android.nfc.tech.NfcB":
										using (NfcB NfcB = NfcB.Get(Tag))
										{
											await NfcB.ConnectAsync();
											try
											{
												byte[] Data = NfcB.GetApplicationData();
												byte[] ProtocolInfo = NfcB.GetProtocolInfo();

												// TODO: Embed in NfcB class
											}
											finally
											{
												NfcB.Close();
											}
										}
										break;

									case "android.nfc.tech.NfcBarcode":
										using (NfcBarcode NfcBarcode = NfcBarcode.Get(Tag))
										{
											await NfcBarcode.ConnectAsync();
											try
											{
												byte[] Data = NfcBarcode.GetBarcode();

												// TODO: Embed in NfcBarcode class
											}
											finally
											{
												NfcBarcode.Close();
											}
										}
										break;

									case "android.nfc.tech.NfcF":
										using (NfcF NfcF = NfcF.Get(Tag))
										{
											await NfcF.ConnectAsync();
											try
											{
												byte[] Data = NfcF.GetManufacturer();
												byte[] ProtocolInfo = NfcF.GetSystemCode();

												// TODO: Embed in NfcF class
											}
											finally
											{
												NfcF.Close();
											}
										}
										break;

									case "android.nfc.tech.NfcV":
										using (NfcV NfcV = NfcV.Get(Tag))
										{
											await NfcV.ConnectAsync();
											try
											{
												sbyte DsfId = NfcV.DsfId;
												sbyte ResponseFlags = NfcV.ResponseFlags;

												// TODO: Embed in NfcV class
											}
											finally
											{
												NfcV.Close();
											}
										}
										break;
								}
							}
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