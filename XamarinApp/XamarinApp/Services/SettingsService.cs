using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Waher.Persistence;
using Waher.Persistence.Filters;
using XamarinApp.Services;

namespace XamarinApp.Services
{
    internal sealed class SettingsService : ISettingsService
    {
        public async Task SaveState(string key, object state)
        {
                var existingState = await Database.FindFirstIgnoreRest<SettingsState>(key);
                if (existingState != null)
                {
                    existingState.Value = JsonConvert.SerializeObject(state);
                    await Database.Update(existingState);
                }
                else
                {
                    var newState = new SettingsState();
                    newState.Key = key;
                    newState.Value = JsonConvert.SerializeObject(state);
                    await Database.Insert(newState);
                }
        }

        public async Task<IEnumerable<(string key, T value)>> RestoreStateWhere<T>(Func<string, bool> predicate)
        {
            List<SettingsState> existingStates = (await Database.Find<SettingsState>()).ToList();
            List<SettingsState> matchingStates = new List<SettingsState>();
            foreach (var state in existingStates)
            {
                if (predicate(state.Key))
                {
                    matchingStates.Add(state);
                }
            }

            return matchingStates.Select(x => (x.Key, JsonConvert.DeserializeObject<T>(x.Value)));
        }

        public async Task<T> RestoreState<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default;

            var existingState = await Database.FindFirstIgnoreRest<SettingsState>(new FilterFieldEqualTo(SettingsState.KeyName, key));
            if (existingState != null)
            {
                return JsonConvert.DeserializeObject<T>(existingState.Value);
            }

            return default;
        }

        public async Task RemoveState<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            await Database.FindDelete<SettingsState>(new FilterFieldEqualTo(SettingsState.KeyName, key));
        }
    }

    internal sealed class SettingsState
    {
        public const string KeyName = "Key";
        public string Key { get; set; }
        public string Value { get; set; }
    }
}