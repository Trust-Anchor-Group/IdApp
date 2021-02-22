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

		/// <inheritdoc/>
		public override Task Queue(Event @event)
		{
			if (@event.Type >= EventType.Error)
			{
				Dictionary<string, string> crashParameters = new Dictionary<string, string>()
				{
					{ "Timestamp", @event.Timestamp.ToString() },
					{ "EventId", @event.EventId },
					{ "Level", @event.Level.ToString() },
					{ "Type", @event.Type.ToString() },
					{ "Message", @event.Message },
					{ "Object", @event.Object },
					{ "Actor", @event.Actor },
					{ "Module", @event.Module },
					{ "Facility", @event.Facility },
					{ "StackTrace", @event.StackTrace },
				};

				if (!(@event.Tags is null))
				{
					foreach (KeyValuePair<string, object> tag in @event.Tags)
						crashParameters[tag.Key] = tag.Value?.ToString() ?? string.Empty;
				}

				// Add base properties like app version etc.
				foreach (KeyValuePair<string, string> p in this.logService.GetParameters())
					crashParameters[p.Key] = p.Value;

				//Microsoft.AppCenter.Crashes.Crashes.TrackError(exception, crashParameters);
			}

			return Task.CompletedTask;
		}
	}
}