using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Networking.XMPP;
using Xamarin.Forms;

namespace Tag.Sdk.Core.Services
{
    internal sealed class LogService : IInternalLogService
    {
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

        public void LogException(Exception e, params KeyValuePair<string, string>[] extraParameters)
        {
            var parameters = GetParameters();
            if (extraParameters != null && extraParameters.Length > 0)
            {
                foreach (var extraParameter in extraParameters)
                {
                    parameters.Add(extraParameter.Key, extraParameter.Value);
                }
            }
            Crashes.TrackError(e, parameters);
            Log.Critical(e, string.Empty, this.bareJid, parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)).ToArray());

            foreach (ILogListener listener in this.listeners)
            {
                listener.LogException(e, extraParameters);
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

        public void LogException(Exception e)
        {
            LogException(e, null);
        }

        public void LogEvent(string name, params KeyValuePair<string, string>[] extraParameters)
        {
            if (extraParameters != null)
            {
                Analytics.TrackEvent(name, extraParameters.ToDictionary(x => x.Key, x => x.Value));
            }
            else
            {
                Analytics.TrackEvent(name);
            }

            foreach (ILogListener listener in this.listeners)
            {
                listener.LogEvent(name, extraParameters);
            }
        }

        private Dictionary<string, string> GetParameters()
        {
            return new Dictionary<string, string>
            {
                {"Platform", Device.RuntimePlatform },
                {"RuntimeVersion", typeof(LogService).Assembly.ImageRuntimeVersion },
                {"AppVersion", this.appInformation.GetVersion() }
            };
        }
    }
}