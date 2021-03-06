﻿using System;
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
            try
            {
                await RuntimeSettings.SetAsync(key, state);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task SaveState(string key, long state)
        {
            try
            {
                await RuntimeSettings.SetAsync(key, state);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task SaveState(string key, double state)
        {
            try
            {
                await RuntimeSettings.SetAsync(key, state);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task SaveState(string key, bool state)
        {
            try
            {
                await RuntimeSettings.SetAsync(key, state);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task SaveState(string key, DateTime state)
        {
            try
            {
                await RuntimeSettings.SetAsync(key, state);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task SaveState(string key, TimeSpan state)
        {
            try
            {
                await RuntimeSettings.SetAsync(key, state);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task SaveState(string key, Enum state)
        {
            try
            {
                await RuntimeSettings.SetAsync(key, state);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task SaveState(string key, object state)
        {
            try
            {
                await RuntimeSettings.SetAsync(key, state);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task<IEnumerable<(string key, T value)>> RestoreStateWhereKeyStartsWith<T>(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                return Array.Empty<(string, T)>();
            }

            List<(string, T)> matches = new List<(string, T)>();

            try
            {
                keyPrefix = FormatKey(keyPrefix);
                Dictionary<string, object> existingStates = (await RuntimeSettings.GetWhereKeyLikeAsync(keyPrefix, WildCard));

                foreach (var state in existingStates)
                {
                    if (state.Value is T typedValue)
                        matches.Add((state.Key, typedValue));
                }
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return matches;
        }

        public async Task<string> RestoreStringState(string key, string defaultValueIfNotFound = default)
        {
            try
            {
                string str = await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
                str = str?.Trim('"');
                return str;
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return defaultValueIfNotFound;
        }

        public async Task<long> RestoreLongState(string key, long defaultValueIfNotFound = default)
        {
            try
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return defaultValueIfNotFound;
        }

        public async Task<double> RestoreDoubleState(string key, double defaultValueIfNotFound = default)
        {
            try
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return defaultValueIfNotFound;
        }

        public async Task<bool> RestoreBoolState(string key, bool defaultValueIfNotFound = default)
        {
            try
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return defaultValueIfNotFound;
        }

        public async Task<DateTime> RestoreDateTimeState(string key, DateTime defaultValueIfNotFound = default)
        {
            try
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return defaultValueIfNotFound;
        }

        public async Task<TimeSpan> RestoreTimeSpanState(string key, TimeSpan defaultValueIfNotFound = default)
        {
            try
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return defaultValueIfNotFound;
        }

        public async Task<Enum> RestoreEnumState(string key, Enum defaultValueIfNotFound = default)
        {
            try
            {
                return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return defaultValueIfNotFound;
        }

        public async Task<T> RestoreState<T>(string key, T defaultValueIfNotFound = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValueIfNotFound;


            try
            {
                object existingState = await RuntimeSettings.GetAsync(key, (object)null);

                if (existingState is T typedValue)
                    return typedValue;
                return defaultValueIfNotFound;
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }

            return defaultValueIfNotFound;
        }

        public async Task RemoveState(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            try
            {
                await RuntimeSettings.DeleteAsync(key);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public async Task RemoveStateWhereKeyStartsWith(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
                return;

            try
            {
                keyPrefix = FormatKey(keyPrefix);
                await RuntimeSettings.DeleteWhereKeyLikeAsync(keyPrefix, WildCard);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        public Task<bool> WaitInitDone()
		{
            return this.storageService.WaitInitDone();
		}
    }
}