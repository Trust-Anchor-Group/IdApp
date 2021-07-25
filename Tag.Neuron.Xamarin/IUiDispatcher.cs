using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin
{
    /// <summary>
    /// A wafer-thin wrapper around the UI (main) thread.
    /// Provides methods for displaying alerts to the user in a thread-safe way. Queues them up if there's more than one.
    /// </summary>
    [DefaultImplementation(typeof(UiDispatcher))]
    public interface IUiDispatcher
    {
        /// <summary>
        /// Does a begin-invoke on the main thread of the specified action.
        /// </summary>
        /// <param name="action">The action to execute asynchronously.</param>
        void BeginInvokeOnMainThread(Action action);

        /// <summary>
        /// Determines whether the app is running in the background.
        /// </summary>
        bool IsRunningInTheBackground { get; set; }

        /// <summary>
        /// Displays an alert/message box to the user.
        /// </summary>
        /// <param name="title">The title to display.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="accept">The accept/ok button text.</param>
        /// <param name="cancel">The cancel button text.</param>
        /// <returns>If Accept or Cancel was pressed</returns>
        Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
        
        /// <summary>
        /// Displays an alert/message box to the user.
        /// </summary>
        /// <param name="title">The title to display.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="accept">The accept/ok button text.</param>
        Task DisplayAlert(string title, string message, string accept);
        
        /// <summary>
        /// Displays an alert/message box to the user.
        /// </summary>
        /// <param name="title">The title to display.</param>
        /// <param name="message">The message to display.</param>
        Task DisplayAlert(string title, string message);
        
        /// <summary>
        /// Displays an alert/message box to the user.
        /// </summary>
        /// <param name="title">The title to display.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="exception">The exception to display.</param>
        Task DisplayAlert(string title, string message, Exception exception);
        
        /// <summary>
        /// Displays an alert/message box to the user.
        /// </summary>
        /// <param name="title">The title to display.</param>
        /// <param name="exception">The exception to display.</param>
        Task DisplayAlert(string title, Exception exception);
        
        /// <summary>
        /// Displays an alert/message box to the user.
        /// </summary>
        /// <param name="exception">The exception to display.</param>
        Task DisplayAlert(Exception exception);
    }
}