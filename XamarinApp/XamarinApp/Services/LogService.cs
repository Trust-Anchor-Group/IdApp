using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Xamarin.Forms;

namespace XamarinApp.Services
{
    internal sealed class LogService : ILogService
    {
        private Dictionary<string, string> GetParameters()
        {
            return new Dictionary<string, string>
            {
                {"Platform", Device.RuntimePlatform }
            };
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
        }

        public void LogException(Exception e)
        {
            var parameters = GetParameters();
            Crashes.TrackError(e, parameters);
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