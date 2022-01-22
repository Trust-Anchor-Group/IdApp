using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using IdApp.DeviceSpecific;
using IdApp.Services.Nfc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Waher.Events;
using Waher.Runtime.Inventory;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.NearFieldCommunication))]
namespace IdApp.Android
{
	/// <summary>
	/// Class managing Near-Fieald Communication (NFC) on Android.
	/// </summary>
	public class NearFieldCommunication : INearFieldCommunication
	{
		private static MainActivity activity = null;
		private static NfcAdapter adapter = null;
		private static bool isListening = false;

		public NearFieldCommunication()
		{
		}

		public static void OnCreated(MainActivity Activity)
		{
			activity = Activity;
			adapter = NfcAdapter.GetDefaultAdapter(Application.Context);
		}

		public static void OnResumed(MainActivity Activity)
		{
			OnCreated(Activity);
		}

		public static void OnNewIntent(Intent Intent)
		{
			INfcService NfcService = Types.Instantiate<INfcService>(true);
		}

		public bool Available
		{
			get
			{
				if (adapter is null)
					return false;

				if (Application.Context.CheckCallingOrSelfPermission(Manifest.Permission.Nfc) != Permission.Granted)
					return false;

				return true;

			}
		}

		public bool Enabled => this.Available && adapter.IsEnabled;
		public bool SupportsWriting => this.Enabled;

		public bool Listening
		{
			get => isListening;
			private set
			{
				if (isListening != value)
				{
					isListening = value;
					this.RaisePropertyChanged(nameof(Listening));
				}
			}
		}

		public void StartListening()
		{
			if (adapter == null || isListening)
				return;

			Intent intent = new Intent(Application.Context, Application.Context.GetType()).AddFlags(ActivityFlags.SingleTop);
			PendingIntent pendingIntent = PendingIntent.GetActivity(Application.Context, 0, intent, 0);

			IntentFilter ndefFilter = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
			ndefFilter.AddDataType("*/*");

			var tagFilter = new IntentFilter(NfcAdapter.ActionTagDiscovered);
			tagFilter.AddCategory(Intent.CategoryDefault);

			var filters = new IntentFilter[] { ndefFilter, tagFilter };

			adapter.EnableForegroundDispatch(activity, pendingIntent, filters, null);

			Listening = true;
			this.RaisePropertyChanged(nameof(Listening));
		}

		public void StopListening()
		{
			if (adapter is null || !isListening)
				return;

			// TODO: DisablePublishing();
			adapter.DisableForegroundDispatch(activity);

			Listening = false;
			this.RaisePropertyChanged(nameof(Listening));
		}

		private void RaisePropertyChanged(string Property)
		{
			PropertyChangedEventHandler h = this.PropertyChanged;

			if (!(h is null))
			{
				try
				{
					h(this, new PropertyChangedEventArgs(Property));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

	}
}