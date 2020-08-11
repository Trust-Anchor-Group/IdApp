using System;

namespace XamarinApp
{
    /// <summary>
    /// Dependency interface for closing the app.
    /// </summary>
    public interface ICloseApplication
    {
        /// <summary>
        /// Closes the App.
        /// </summary>
        void CloseApplication();
    }
}
