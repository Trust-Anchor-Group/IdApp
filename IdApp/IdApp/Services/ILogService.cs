using System;
using System.Collections.Generic;
using Waher.Events;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
    /// <summary>
    /// A log implementation for logging warnings, exceptions and events.
    /// </summary>
    [DefaultImplementation(typeof(LogService))]
    public interface ILogService
    {
        /// <summary>
        /// Adds an <see cref="IEventSink"/> to the log service. Any listeners will be called
        /// whenever any log event occurs.
        /// </summary>
        /// <param name="eventSink">The listener to add.</param>
        void AddListener(IEventSink eventSink);

        /// <summary>
        /// Removes an <see cref="IEventSink"/> to the log service.
        /// </summary>
        /// <param name="eventSink">The listener to remove.</param>
        void RemoveListener(IEventSink eventSink);
        
        /// <summary>
        /// Invoke this method to add a warning statement to the log.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">The objects to format.</param>
        void LogWarning(string format, params object[] args);
        
        /// <summary>
        /// Invoke this method to add an <see cref="Exception"/> entry to the log.
        /// </summary>
        /// <param name="e"></param>
        void LogException(Exception e);
        
        /// <summary>
        /// Invoke this method to add an <see cref="Exception"/> entry to the log.
        /// </summary>
        /// <param name="e">The exception to log.</param>
        /// <param name="extraParameters">Any extra parameters that are added to the log.</param>
        void LogException(Exception e, params KeyValuePair<string, string>[] extraParameters);
        
        /// <summary>
        /// Invoke this method to add an event entry to the log.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="extraParameters">Any extra parameters that are added to the log.</param>
        void LogEvent(string name, params KeyValuePair<string, string>[] extraParameters);

        /// <summary>
        /// Saves an exception dump to disc, completely offline. A last resort operation.
        /// </summary>
        /// <param name="title">The title of the stack trace.</param>
        /// <param name="stackTrace">The actual stack trace.</param>
        void SaveExceptionDump(string title, string stackTrace);
        
        /// <summary>
        /// Restores any exception dump that has previously been persisted with the <see cref="SaveExceptionDump"/> method.
        /// </summary>
        /// <returns>The exception dump, if it exists, or <c>null</c>.</returns>
        string LoadExceptionDump();
        
        /// <summary>
        /// Removes any exception dump from disc, if it exists.
        /// </summary>
        void DeleteExceptionDump();

        /// <summary>
        /// Gets a list of extra parameters that are useful when logging: Platform, RuntimeVersion, AppVersion.
        /// </summary>
        /// <returns>Parameters</returns>
        IList<KeyValuePair<string, string>> GetParameters();
    }
}