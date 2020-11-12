using System.Threading.Tasks;
using Newtonsoft.Json;
using Waher.Runtime.Settings;

namespace XamarinApp.Services
{
    internal sealed class SettingsService : ISettingsService
    {
        public async Task SaveState(string key, object state)
        {
            await RuntimeSettings.SetAsync(key, JsonConvert.SerializeObject(state));
        }

        public async Task<T> RestoreState<T>(string key, T defaultValue = default(T))
        {
            string value = await RuntimeSettings.GetAsync(key, null);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return JsonConvert.DeserializeObject<T>(value);
            }

            return defaultValue;
        }

        public async Task RemoveState<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            await RuntimeSettings.DeleteAsync(key);
        }
    }
}