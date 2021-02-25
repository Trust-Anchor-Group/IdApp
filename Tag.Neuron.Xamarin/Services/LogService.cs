using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
    internal sealed class LogService : ILogService
    {
        private const string StartupCrashFileName = "CrashDump.txt";
        private string bareJid = string.Empty;

        public void AddListener(IEventSink eventSink)
        {
            if (eventSink is XmppEventSink xmppEventSink)
            {
                this.bareJid = xmppEventSink.Client?.BareJID;
            }
            Log.Register(eventSink);
        }

        public void RemoveListener(IEventSink eventSink)
        {
            if (eventSink != null)
            {
                Log.Unregister(eventSink);
            }
        }

        public void LogException(Exception e, params KeyValuePair<string, string>[] extraParameters)
        {
			e = Log.UnnestException(e);

            var parameters = GetParameters();
            if (extraParameters != null && extraParameters.Length > 0)
            {
                foreach (var extraParameter in extraParameters)
                {
                    parameters.Add(new KeyValuePair<string, string>(extraParameter.Key, extraParameter.Value));
                }
            }

            try
            {
                Log.Critical(e, string.Empty, this.bareJid, parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToArray());
            }
            catch (Exception exception)
            {
                this.SaveExceptionDump($"{nameof(LogException)} calls Waher.Events.Log.Critical()", exception.ToString());
            }
        }

        public void LogWarning(string format, params object[] args)
        {
            string message = string.Format(format, args);
            var parameters = GetParameters();
            try
            {
                Log.Warning(message, string.Empty, this.bareJid, parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToArray());
            }
            catch (Exception exception)
            {
                this.SaveExceptionDump($"{nameof(LogWarning)} calls Waher.Events.Log.Warning()", exception.ToString());
            }
        }

        public void LogException(Exception e)
        {
            LogException(e, null);
        }

        public void LogEvent(string name, params KeyValuePair<string, string>[] extraParameters)
        {
            var parameters = GetParameters();
            if (extraParameters != null && extraParameters.Length > 0)
            {
                foreach (var extraParameter in extraParameters)
                {
                    parameters.Add(new KeyValuePair<string, string>(extraParameter.Key, extraParameter.Value));
                }
            }

            try
            {
                var tags = parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToArray();
                Log.Event(new Event(DateTime.UtcNow, EventType.Informational, name, string.Empty, this.bareJid, string.Empty, EventLevel.Medium, string.Empty, string.Empty, string.Empty, tags));
            }
            catch (Exception exception)
            {
                this.SaveExceptionDump($"{nameof(LogEvent)} calls Waher.Events.Log.Event()", exception.ToString());
            }
        }

        public void SaveExceptionDump(string title, string stackTrace)
        {
            stackTrace = Log.CleanStackTrace(stackTrace);

            string contents;
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StartupCrashFileName);
            if (File.Exists(fileName))
            {
                contents = File.ReadAllText(fileName);
            }
            else
            {
                contents = string.Empty;
            }

            File.WriteAllText(fileName, $"{title}{Environment.NewLine}{stackTrace}{Environment.NewLine}{contents}");
        }

        public string LoadExceptionDump()
        {
            string contents;
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StartupCrashFileName);
            if (File.Exists(fileName))
            {
                contents = File.ReadAllText(fileName);
            }
            else
            {
                contents = string.Empty;
            }

            return contents;
        }

        public void DeleteExceptionDump()
        {
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StartupCrashFileName);
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