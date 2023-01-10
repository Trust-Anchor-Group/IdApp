using IdApp.Services.Push;
using System.Threading.Tasks;

namespace IdApp.DeviceSpecific
{
    /// <summary>
    /// Dependency interface for device-specific push notification tokens.
    /// </summary>
    public interface IGetPushNotificationToken
    {
        /// <summary>
        /// Gets a Push Notification token for the device.
        /// </summary>
        /// <returns>Token information.</returns>
        Task<TokenInformation> GetToken();
    }
}