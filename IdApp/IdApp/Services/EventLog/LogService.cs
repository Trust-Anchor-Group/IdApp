using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Services.EventLog
{
	[Singleton]
	internal sealed class LogService : ILogService
	{
		private const string startupCrashFileName = "CrashDump.txt";
		private string bareJid = string.Empty;

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

		public void LogException(Exception e)
		{
			this.LogException(e, null);
		}

		public void LogException(Exception e, params KeyValuePair<string, string>[] extraParameters)
		{
			e = Log.UnnestException(e);

			IList<KeyValuePair<string, string>> parameters = this.GetParameters();

			if (!(extraParameters is null) && extraParameters.Length > 0)
			{
				foreach (KeyValuePair<string, string> extraParameter in extraParameters)
					parameters.Add(new KeyValuePair<string, string>(extraParameter.Key, extraParameter.Value));
			}

			Log.Critical(e, string.Empty, this.bareJid, parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToArray());
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
