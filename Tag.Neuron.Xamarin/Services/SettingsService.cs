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
        private const string EmptyJson = "{}";

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

        public async Task SaveState(string key, object state)
        {
            string stateValue = state != null ? Waher.Content.JSON.Encode(state, true) : EmptyJson;
            await RuntimeSettings.SetAsync(key, stateValue);
        }

        public async Task<IEnumerable<(string key, T value)>> RestoreStateWhereKeyStartsWith<T>(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                return Array.Empty<(string, T)>();
            }

            keyPrefix = FormatKey(keyPrefix);

            var existingStates = (await RuntimeSettings.GetWhereKeyLikeAsync(keyPrefix, WildCard));
            List<(string, T)> matches = new List<(string, T)>();
            foreach (var state in existingStates)
            {
                matches.Add((state.Key, (T)Waher.Content.JSON.Parse((string)state.Value)));
            }

            return matches;
        }

        public async Task<T> RestoreState<T>(string key, T defaultValueIfNotFound = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValueIfNotFound;

            string existingState = await RuntimeSettings.GetAsync(key, EmptyJson);

            if (!string.IsNullOrWhiteSpace(existingState) && existingState != EmptyJson)
            {
                return (T)Waher.Content.JSON.Parse(existingState);
            }

            return defaultValueIfNotFound;
        }

        public async Task RemoveState(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            await RuntimeSettings.DeleteAsync(key);
        }

        public async Task RemoveStateWhereKeyStartsWith(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
                return;

            keyPrefix = FormatKey(keyPrefix);
            await RuntimeSettings.DeleteWhereKeyLikeAsync(keyPrefix, WildCard);
        }
    }
}