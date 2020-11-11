using System.Threading.Tasks;

namespace XamarinApp.Services
{
    public interface ISettingsService
    {
        Task SaveState(string key, object state);
        Task<T> RestoreState<T>(string key, T defaultValue = default(T));
        Task RemoveState<T>(string key);
    }
}