using System;
using System.Collections.Generic;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// A log listener to implement and register with the <see cref="ILogService"/> to get notifications
    /// and being able to piggy back other calls whenever a log event occurs.
    /// </summary>
    public interface ILogListener
    {
        /// <summary>
        /// Called whenever a warning is added to the <see cref="ILogService"/>.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">The objects to format.</param>
        void LogWarning(string format, params object[] args);
        /// <summary>
        /// Called whenever an <see cref="Exception"/> is added to the <see cref="ILogService"/>.
        /// </summary>
        /// <param name="e">The exception to log.</param>
        void LogException(Exception e);
        /// <summary>
        /// Called whenever an <see cref="Exception"/> is added to the <see cref="ILogService"/>.
        /// </summary>
        /// <param name="e">The exception to log.</param>
        /// <param name="extraParameters">Any extra parameters that are added to the log.</param>
        void LogException(Exception e, params KeyValuePair<string, string>[] extraParameters);
        /// <summary>
        /// Called whenever an event is added to the <see cref="ILogService"/>.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="extraParameters">Any extra parameters that are added to the log.</param>
        void LogEvent(string name, params KeyValuePair<string, string>[] extraParameters);
    }
}