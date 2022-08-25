using IdApp.DeviceSpecific;
using IdApp.Services.Storage;
using IdApp.Services.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Persistence.Exceptions;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Services.EventLog
{
	[Singleton]
	internal sealed class LogService : ILogService
	{
		private const string startupCrashFileName = "CrashDump.txt";
		private string bareJid = string.Empty;
		private bool repairRequested = false;

		public void AddListener(IEventSink eventSink)
		{
			if (eventSink is XmppEventSink xmppEventSink)
				this.bareJid = xmppEventSink.Client?.BareJID;

			foreach (IEventSink Sink in Log.Sinks)
			{
				if (Sink == eventSink)
					return;
			}

			Log.Register(eventSink);
		}

		public void RemoveListener(IEventSink eventSink)
		{
			if (!(eventSink is null))
				Log.Unregister(eventSink);
		}

		public void LogWarning(string format, params object[] args)
		{
			string message = string.Format(format, args);
			IList<KeyValuePair<string, string>> parameters = this.GetParameters();

			Log.Warning(message, string.Empty, this.bareJid, parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToArray());
		}

		public void LogException(Exception ex)
		{
			this.LogException(ex, null);
		}

		public void LogException(Exception ex, params KeyValuePair<string, string>[] extraParameters)
		{
			ex = Log.UnnestException(ex);

			IList<KeyValuePair<string, string>> parameters = this.GetParameters();

			if (!(extraParameters is null) && extraParameters.Length > 0)
			{
				foreach (KeyValuePair<string, string> extraParameter in extraParameters)
					parameters.Add(new KeyValuePair<string, string>(extraParameter.Key, extraParameter.Value));
			}

			Log.Critical(ex, string.Empty, this.bareJid, parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToArray());

			if (ex is InconsistencyException && !this.repairRequested)
			{
				this.repairRequested = true;
				Task.Run(() => this.RestartForRepair());
			}
		}

		private async Task RestartForRepair()
		{
			IStorageService StorageService = App.Instantiate<IStorageService>();
			StorageService.FlagForRepair();

			IUiSerializer UiSerializer = App.Instantiate<IUiSerializer>();
			await UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["RepairRestart"], LocalizationResourceManager.Current["Ok"]);

			ICloseApplication CloseApplication = DependencyService.Get<ICloseApplication>();
			await CloseApplication.Close();
		}

		public void SaveExceptionDump(string title, string stackTrace)
		{
			stackTrace = Log.CleanStackTrace(stackTrace);

			string contents;
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

			if (File.Exists(fileName))
				contents = File.ReadAllText(fileName);
			else
				contents = string.Empty;

			File.WriteAllText(fileName, title + Environment.NewLine + stackTrace + Environment.NewLine + contents);
		}

		public string LoadExceptionDump()
		{
			string contents;
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

			if (File.Exists(fileName))
				contents = File.ReadAllText(fileName);
			else
				contents = string.Empty;

			return contents;
		}

		public void DeleteExceptionDump()
		{
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), startupCrashFileName);

			if (File.Exists(fileName))
				File.Delete(fileName);
		}

		///<inheritdoc/>
		public IList<KeyValuePair<string, string>> GetParameters()
		{
			return new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("Platform", Device.RuntimePlatform),
				new KeyValuePair<string, string>("RuntimeVersion", typeof(LogService).Assembly.ImageRuntimeVersion),
				new KeyValuePair<string, string>("AppVersion", AppInfo.VersionString),
				new KeyValuePair<string, string>("Manufacturer", DeviceInfo.Manufacturer),
				new KeyValuePair<string, string>("Device Model", DeviceInfo.Model),
				new KeyValuePair<string, string>("Device Name", DeviceInfo.Name),
				new KeyValuePair<string, string>("OS", DeviceInfo.VersionString),
				new KeyValuePair<string, string>("Platform", DeviceInfo.Platform.ToString()),
				new KeyValuePair<string, string>("Device Type", DeviceInfo.DeviceType.ToString()),
			};
		}
	}
}
