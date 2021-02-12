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
    public class AppCenterEventSink : IEventSink
    {
        private static readonly string AppCenterSinkId = Guid.NewGuid().ToString();

        private readonly ILogService logService;

        /// <summary>
        /// Creates an instance of the <see cref="AppCenterEventSink"/> class.
        /// </summary>
        /// <param name="logService">The log service to use.</param>
        public AppCenterEventSink(ILogService logService)
        {
            this.logService = logService;
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
        }

        public void LogDebug(string message, string actor, string eventId, EventLevel level, string facility, string module, string stackTrace, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogDebug(string message, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogDebug(string message, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogDebug(string message, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogDebug(string message, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogDebug(string message, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogDebug(string message, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogInformational(string message, string actor, string eventId, EventLevel level, string facility, string module, string stackTrace, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogInformational(string message, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogInformational(string message, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogInformational(string message, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogInformational(string message, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogInformational(string message, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogInformational(string message, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogNotice(string message, string actor, string eventId, EventLevel level, string facility, string module,
            string stackTrace, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogNotice(string message, string actor, string eventId, EventLevel level, string facility, string module,
            params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogNotice(string message, string actor, string eventId, EventLevel level, string facility,
            params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogNotice(string message, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogNotice(string message, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogNotice(string message, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogNotice(string message, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogWarning(string message, string actor, string eventId, EventLevel level, string facility, string module,
            string stackTrace, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogWarning(string message, string actor, string eventId, EventLevel level, string facility, string module,
            params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogWarning(string message, string actor, string eventId, EventLevel level, string facility,
            params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogWarning(string message, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogWarning(string message, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogWarning(string message, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogWarning(string message, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(string message, string actor, string eventId, EventLevel level, string facility, string module,
            string stackTrace, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(string message, string actor, string eventId, EventLevel level, string facility, string module,
            params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(string message, string actor, string eventId, EventLevel level, string facility,
            params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(string message, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(string message, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(string message, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(string message, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(Exception exception, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
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

        public void LogError(Exception exception, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(Exception exception, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(Exception exception, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(Exception exception, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogError(Exception exception, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(string message, string actor, string eventId, EventLevel level, string facility, string module, string stackTrace, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(string message, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(string message, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(string message, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(string message, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(string message, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(string message, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(Exception exception, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(Exception exception, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(Exception exception, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(Exception exception, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(Exception exception, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogCritical(Exception exception, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(string message, string actor, string eventId, EventLevel level, string facility, string module, string stackTrace, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(string message, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(string message, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(string message, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(string message, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(string message, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(string message, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(Exception exception, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(Exception exception, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(Exception exception, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(Exception exception, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(Exception exception, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogAlert(Exception exception, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(string message, string actor, string eventId, EventLevel level, string facility, string module, string stackTrace, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(string message, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(string message, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(string message, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(string message, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(string message, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(string message, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(Exception exception, string actor, string eventId, EventLevel level, string facility, string module, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(Exception exception, string actor, string eventId, EventLevel level, string facility, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(Exception exception, string actor, string eventId, EventLevel level, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(Exception exception, string actor, string eventId, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(Exception exception, string actor, params KeyValuePair<string, object>[] tags)
        {
        }

        public void LogEmergency(Exception exception, params KeyValuePair<string, object>[] tags)
        {
        }

        /// <inheritdoc/>
        public string ObjectID => AppCenterSinkId;

        /// <inheritdoc/>
        public Task Queue(Event @event)
        {
            return Task.CompletedTask;
        }
    }
}