using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Tag.Neuron.Xamarin.Services;

namespace IdApp
{
    public class AppCenterLogListener : ILogListener
    {
        public void LogWarning(string format, params object[] args)
        {
            // Not implemented
        }

        public void LogException(Exception e)
        {
            LogException(e, null);
        }

        public void LogException(Exception e, params KeyValuePair<string, string>[] extraParameters)
        {
            if (extraParameters != null)
            {
                Dictionary<string, string> crashParameters = extraParameters.GroupBy(p => p.Key).Select(g => g.First()).ToDictionary(k => k.Key, v => v.Value);
                Crashes.TrackError(e, crashParameters);
            }
            else
            {
                Crashes.TrackError(e);
            }
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
    }
}