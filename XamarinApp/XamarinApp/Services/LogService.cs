using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Networking.XMPP;
using Xamarin.Forms;

namespace XamarinApp.Services
{
    internal sealed class LogService : IInternalLogService
    {
        private XmppEventSink eventSink;
        private readonly IAppInformation appInformation;
        private string bareJid = string.Empty;

        public LogService(IAppInformation appInformation)
        {
            this.appInformation = appInformation;
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

        public void UnregisterEventSink()
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