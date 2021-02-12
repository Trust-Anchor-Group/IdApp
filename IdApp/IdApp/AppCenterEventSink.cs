using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Waher.Events;

namespace IdApp
{
    /// <summary>
    /// An event sink for reporting events to Microsoft App Center Analytics and Crashes.
    /// </summary>
    public class AppCenterEventSink : EventSink
    {
        private readonly ILogService logService;

        /// <summary>
        /// Creates an instance of the <see cref="AppCenterEventSink"/> class.
        /// </summary>
        /// <param name="logService">The log service to use.</param>
        public AppCenterEventSink(ILogService logService)
        : base("App Center Event Sink")
        {
            this.logService = logService;
        }

        ///<inheritdoc cref="LogObject"/>
        public new void LogError(Exception exception, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
            // Uncomment when adding a reference to Microsoft AppCenter
            if (tags != null)
            {
                Dictionary<string, string> crashParameters = tags.GroupBy(p => p.Key).Select(g => g.First()).ToDictionary(k => k.Key, v => v.Value.ToString());
                crashParameters.AddToDictionary(nameof(actor), actor);
                crashParameters.AddToDictionary(nameof(eventId), eventId);
                crashParameters.AddToDictionary(nameof(level), level.ToString());
                crashParameters.AddToDictionary(nameof(facility), facility);
                crashParameters.AddToDictionary(nameof(module), module);
                crashParameters.AddToDictionary(nameof(actor), actor);
                // Add base properties like app version et.c.
                foreach (KeyValuePair<string, string> p in this.logService.GetParameters())
                {
                    crashParameters[p.Key] = p.Value;
                }
                //Microsoft.AppCenter.Crashes.Crashes.TrackError(exception, crashParameters);
            }
            //else
            //{
            //Microsoft.AppCenter.Crashes.Crashes.TrackError(exception);
            //}
        }

        /// <inheritdoc/>
        public override Task Queue(Event @event)
        {
            return Task.CompletedTask;
        }
    }
}