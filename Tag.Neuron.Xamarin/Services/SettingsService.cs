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
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, long state)
		{
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, double state)
		{
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, bool state)
		{
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, DateTime state)
		{
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, TimeSpan state)
		{
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, object state)
		{
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task<IEnumerable<(string key, T value)>> RestoreStateWhereKeyStartsWith<T>(string keyPrefix)
		{
			if (string.IsNullOrWhiteSpace(keyPrefix))
			{
				return Array.Empty<(string, T)>();
			}

			keyPrefix = FormatKey(keyPrefix);

			Dictionary<string, object> existingStates = (await RuntimeSettings.GetWhereKeyLikeAsync(keyPrefix, WildCard));
			List<(string, T)> matches = new List<(string, T)>();

			foreach (var state in existingStates)
			{
				if (state.Value is T typedValue)
					matches.Add((state.Key, typedValue));
			}

			return matches;
		}

		public async Task<string> RestoreStringState(string key, string defaultValueIfNotFound = default)
		{
			string str = await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            str = str?.Trim('"');
            return str;
        }

		public Task<long> RestoreLongState(string key, long defaultValueIfNotFound = default)
		{
			return RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public Task<double> RestoreDoubleState(string key, double defaultValueIfNotFound = default)
		{
			return RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public Task<bool> RestoreBoolState(string key, bool defaultValueIfNotFound = default)
		{
			return RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public Task<DateTime> RestoreDateTimeState(string key, DateTime defaultValueIfNotFound = default)
		{
			return RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public Task<TimeSpan> RestoreTimeSpanState(string key, TimeSpan defaultValueIfNotFound = default)
		{
			return RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public async Task<T> RestoreState<T>(string key, T defaultValueIfNotFound = default)
		{
			if (string.IsNullOrWhiteSpace(key))
				return defaultValueIfNotFound;

			object existingState = await RuntimeSettings.GetAsync(key, null);

            if (existingState is T typedValue)
				return typedValue;
			else
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