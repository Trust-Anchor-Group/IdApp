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

		public SettingsService(IStorageService storageService)
        {
            this.storageService = storageService;
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
            await this.storageService.WaitForReadyState();
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, long state)
		{
            await this.storageService.WaitForReadyState();
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, double state)
		{
            await this.storageService.WaitForReadyState();
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, bool state)
		{
            await this.storageService.WaitForReadyState();
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, DateTime state)
		{
            await this.storageService.WaitForReadyState();
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, TimeSpan state)
		{
            await this.storageService.WaitForReadyState();
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task SaveState(string key, object state)
		{
            await this.storageService.WaitForReadyState();
			await RuntimeSettings.SetAsync(key, state);
		}

		public async Task<IEnumerable<(string key, T value)>> RestoreStateWhereKeyStartsWith<T>(string keyPrefix)
		{
			if (string.IsNullOrWhiteSpace(keyPrefix))
			{
				return Array.Empty<(string, T)>();
			}
            await this.storageService.WaitForReadyState();

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
            await this.storageService.WaitForReadyState();
			string str = await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
            str = str?.Trim('"');
            return str;
        }

		public async Task<long> RestoreLongState(string key, long defaultValueIfNotFound = default)
		{
            await this.storageService.WaitForReadyState();
			return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public async Task<double> RestoreDoubleState(string key, double defaultValueIfNotFound = default)
		{
            await this.storageService.WaitForReadyState();
			return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public async Task<bool> RestoreBoolState(string key, bool defaultValueIfNotFound = default)
		{
            await this.storageService.WaitForReadyState();
			return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public async Task<DateTime> RestoreDateTimeState(string key, DateTime defaultValueIfNotFound = default)
		{
            await this.storageService.WaitForReadyState();
			return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public async Task<TimeSpan> RestoreTimeSpanState(string key, TimeSpan defaultValueIfNotFound = default)
		{
            await this.storageService.WaitForReadyState();
			return await RuntimeSettings.GetAsync(key, defaultValueIfNotFound);
		}

		public async Task<T> RestoreState<T>(string key, T defaultValueIfNotFound = default)
		{
			if (string.IsNullOrWhiteSpace(key))
				return defaultValueIfNotFound;

            await this.storageService.WaitForReadyState();

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
            await this.storageService.WaitForReadyState();
			await RuntimeSettings.DeleteAsync(key);
		}

		public async Task RemoveStateWhereKeyStartsWith(string keyPrefix)
		{
			if (string.IsNullOrWhiteSpace(keyPrefix))
				return;

            await this.storageService.WaitForReadyState();
			keyPrefix = FormatKey(keyPrefix);
			await RuntimeSettings.DeleteWhereKeyLikeAsync(keyPrefix, WildCard);
		}
	}
}