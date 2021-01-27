namespace Tag.Neuron.Xamarin
{
    /// <summary>
    /// Dependency interface for app-specific information.
    /// </summary>
    public interface IAppInformation
    {
        /// <summary>
        /// Returns the version of the app.
        /// </summary>
        /// <returns></returns>
        string GetVersion();
    }
}