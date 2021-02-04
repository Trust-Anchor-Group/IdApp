using System;
using System.Collections.Generic;
using System.Linq;
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
            // Uncomment when adding a reference to Microsoft AppCenter
            //if (extraParameters != null)
            //{
            //    Dictionary<string, string> crashParameters = extraParameters.GroupBy(p => p.Key).Select(g => g.First()).ToDictionary(k => k.Key, v => v.Value);
            //    Microsoft.AppCenter.Crashes.Crashes.TrackError(e, crashParameters);
            //}
            //else
            //{
            //    Microsoft.AppCenter.Crashes.Crashes.TrackError(e);
            //}
        }

        public void LogEvent(string name, params KeyValuePair<string, string>[] extraParameters)
        {
            // Uncomment when adding a reference to Microsoft AppCenter
            //if (extraParameters != null)
            //{
            //    Microsoft.AppCenter.Analytics.Analytics.TrackEvent(name, extraParameters.ToDictionary(x => x.Key, x => x.Value));
            //}
            //else
            //{
            //    Microsoft.AppCenter.Analytics.Analytics.TrackEvent(name);
            //}
        }
    }
}