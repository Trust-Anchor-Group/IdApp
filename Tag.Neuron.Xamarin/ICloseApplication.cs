using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin
{
    /// <summary>
    /// Dependency interface for device-specific closing of application.
    /// </summary>
    public interface ICloseApplication
    {
        /// <summary>
        /// Closes the application
        /// </summary>
        Task Close();
    }
}