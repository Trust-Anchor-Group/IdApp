using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
    internal sealed class SettingsService : ISettingsService
    {
        private const string WildCard = "*";
        private readonly IStorageService storageService;
        private readonly ILogService logService;

        public SettingsService(IStorageService storageService, ILogService logService)
        {
            this.storageService = storageService;
            this.logService = logService;
        }

        private static string FormatKey(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                return WildCard;
            }
            if (!keyPrefix.EndsWith(WildCard))
            {
                keyPrefix += WildCard;
            }

            return keyPrefix;
        }

        public async Task SaveState(string key, string state)
        {
            if (await this.StorageIsReady())
                await RuntimeSettings.SetAsync(key, state);
        }

        public async Task SaveState(string key, long state)
        {
            if (await this.StorageIsReady())
                await RuntimeSettings.SetAsync(key, state);
        }

        public async Task SaveState(string key, double state)
        {
            if (await this.StorageIsReady())
                await RuntimeSettings.SetAsync(key, state);
        }

        public async Task SaveState(string key, bool state)
        {
            if (await this.StorageIsReady())
                await RuntimeSettings.SetAsync(key, state);
        }

        public async Task SaveState(string key, DateTime state)
        {
            if (await this.StorageIsReady())
                await RuntimeSettings.SetAsync(key, state);
        }

        public async Task SaveState(string key, TimeSpan state)
        {
            if (await this.StorageIsReady())
                await RuntimeSettings.SetAsync(key, state);
        }

        public async Task SaveState(string key, Enum state)
        {
            if (await this.StorageIsReady())
                await RuntimeSettings.SetAsync(key, state);
        }

        public async Task SaveState(string key, object state)
        {
            if (await this.StorageIsReady())
                await RuntimeSettings.SetAsync(key, state);
        }

        public async Task<IEnumerable<(string key, T value)>> RestoreStateWhereKeyStartsWith<T>(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                return Array.Empty<(string, T)>();
            }

            List<(string, T)> matches = new List<(string, T)>();

            if (await this.StorageIsReady())
            {
                keyPrefix = FormatKey(keyPrefix);
                Dictionary<string, object> existingStates = (await RuntimeSettings.GetWhereKeyLikeAsync(keyPrefix, WildCard));

                foreach (var state in existingStates)
                {
                    if (state.Value is T typedValue)
                        matches.Add((state.Key, typedValue));
                }
            }

            return matches;
        }

        public async Task<string> RestoreStringState(string key, string defaultValueIfNotFound = default)
        {
            if (await this.StorageIsReady())
            {
                string str = await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
                str = str?.Trim('"');
                return str;
            }
            return defaultValueIfNotFound;
        }

        public async Task<long> RestoreLongState(string key, long defaultValueIfNotFound = default)
        {
            if (await this.StorageIsReady())
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            return defaultValueIfNotFound;
        }

        public async Task<double> RestoreDoubleState(string key, double defaultValueIfNotFound = default)
        {
            if (await this.StorageIsReady())
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            return defaultValueIfNotFound;
        }

        public async Task<bool> RestoreBoolState(string key, bool defaultValueIfNotFound = default)
        {
            if (await this.StorageIsReady())
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            return defaultValueIfNotFound;
        }

        public async Task<DateTime> RestoreDateTimeState(string key, DateTime defaultValueIfNotFound = default)
        {
            if (await this.StorageIsReady())
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            return defaultValueIfNotFound;
        }

        public async Task<TimeSpan> RestoreTimeSpanState(string key, TimeSpan defaultValueIfNotFound = default)
        {
            if (await this.StorageIsReady())
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            return defaultValueIfNotFound;
        }

        public async Task<Enum> RestoreEnumState(string key, Enum defaultValueIfNotFound = default)
        {
            if (await this.StorageIsReady())
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            return defaultValueIfNotFound;
        }

        public async Task<T> RestoreState<T>(string key, T defaultValueIfNotFound = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValueIfNotFound;

            if (await this.StorageIsReady())
            {
                object existingState = await RuntimeSettings.GetAsync(key, (object)null);

                if (existingState is T typedValue)
                    return typedValue;
                return defaultValueIfNotFound;
            }
            return defaultValueIfNotFound;
        }

        public async Task RemoveState(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (await this.StorageIsReady())
                await RuntimeSettings.DeleteAsync(key);
        }

        public async Task RemoveStateWhereKeyStartsWith(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
                return;

            if (await this.StorageIsReady())
            {
                keyPrefix = FormatKey(keyPrefix);
                await RuntimeSettings.DeleteWhereKeyLikeAsync(keyPrefix, WildCard);
            }
        }

        private async Task<bool> StorageIsReady()
        {
            StorageState state = await this.storageService.WaitForReadyState();
            if (state == StorageState.Ready)
            {
                return true;
            }
            this.logService.AddTraceLog("SettingsService.StorageIsReady, state = " + state);

            this.logService.LogWarning($"SettingsService: storage is in state {state}");

            return false;
        }
    }
}