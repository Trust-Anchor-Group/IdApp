﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
    internal sealed class LogService : IInternalLogService
    {
        private const string StartupCrashFileName = "CrashDump.txt";

        private XmppEventSink eventSink;
        private readonly IAppInformation appInformation;
        private string bareJid = string.Empty;
        private readonly IList<ILogListener> listeners;

        public LogService(IAppInformation appInformation)
        {
            this.appInformation = appInformation;
            this.listeners = new List<ILogListener>();
        }

        public void RegisterEventSink(XmppClient client, string logJid)
        {
            if (eventSink == null && !string.IsNullOrWhiteSpace(logJid))
            {
                this.bareJid = client.BareJID ?? string.Empty;
                eventSink = new XmppEventSink("XMPP Event Sink", client, logJid, false);
                Log.Register(eventSink);
            }
        }

        public void UnRegisterEventSink()
        {
            if (eventSink != null)
            {
                Log.Unregister(eventSink);
                eventSink.Dispose();
                eventSink = null;
            }
        }

        public void AddListener(ILogListener listener)
        {
            if (listener != null)
            {
                this.listeners.Add(listener);
            }
        }

        public void RemoveListener(ILogListener listener)
        {
            if (listener != null)
            {
                this.listeners.Remove(listener);
            }
        }

        public void LogException(Exception e, params KeyValuePair<string, string>[] extraParameters)
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
                Log.Critical(e, string.Empty, this.bareJid, parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToArray());
            }
            catch (Exception exception)
            {
                this.SaveExceptionDump($"{nameof(LogException)} calls Waher.Events.Log.Critical()", exception.ToString());
            }

            ILogListener[] listenersClone = this.listeners.ToArray();
            foreach (ILogListener listener in listenersClone)
            {
                try
                {
                    listener.LogException(e, parameters.ToArray());
                }
                catch (Exception exception)
                {
                    this.SaveExceptionDump($"{nameof(LogException)} calls {nameof(ILogListener)} (of type {listener.GetType().FullName})", exception.ToString());
                }
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

            ILogListener[] listenersClone = this.listeners.ToArray();
            foreach (ILogListener listener in listenersClone)
            {
                try
                {
                    listener.LogWarning(format, args);
                }
                catch (Exception exception)
                {
                    this.SaveExceptionDump($"{nameof(LogWarning)} calls {nameof(ILogListener)} (of type {listener.GetType().FullName})", exception.ToString());
                }
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

            ILogListener[] listenersClone = this.listeners.ToArray();
            foreach (ILogListener listener in listenersClone)
            {
                try
                {
                    listener.LogEvent(name, parameters.ToArray());
                }
                catch (Exception exception)
                {
                    this.SaveExceptionDump($"{nameof(LogEvent)} calls {nameof(ILogListener)} (of type {listener.GetType().FullName})", exception.ToString());
                }
            }
        }

        public void SaveExceptionDump(string title, string stackTrace)
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

        private List<KeyValuePair<string, string>> GetParameters()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Platform", Device.RuntimePlatform),
                new KeyValuePair<string, string>("RuntimeVersion", typeof(LogService).Assembly.ImageRuntimeVersion),
                new KeyValuePair<string, string>("AppVersion", this.appInformation.GetVersion())
            };
        }
    }
}