using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
    internal sealed class SettingsService : ISettingsService
    {
        private const SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLiteOpenFlags.SharedCache;

        private static string DatabasePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TagId.db3");

        public SettingsService()
        {
            if (!DesignMode.IsDesignModeEnabled)
            {
                using (var connection = new SQLiteConnection(DatabasePath, Flags))
                {
                    connection.CreateTable<Setting>();
                }
            }
        }

        public Task SaveState(string key, object state)
        {
            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                var existingState = connection.Find<Setting>(key);
                if (existingState != null)
                {
                    existingState.Value = Waher.Content.JSON.Encode(state, true);
                    connection.Update(existingState);
                }
                else
                {
                    var newState = new Setting { Key = key, Value = Waher.Content.JSON.Encode(state, true) };
                    connection.Insert(newState);
                }
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<(string key, T value)>> RestoreStateWhere<T>(Func<string, bool> predicate)
        {
            List<Setting> existingStates;
            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                existingStates = connection.Table<Setting>().ToList();
            }

            List<(string, T)> matches = new List<(string, T)>();
            foreach (var state in existingStates)
            {
                if (predicate(state.Key))
                {
                    matches.Add((state.Key, (T)Waher.Content.JSON.Parse(state.Value)));
                }
            }

            return Task.FromResult((IEnumerable<(string, T)>)matches);
        }

        public Task<T> RestoreState<T>(string key, T defaultValueIfNotFound = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult(defaultValueIfNotFound);

            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                var existingState = connection.Find<Setting>(key);
                if (existingState != null)
                {
                    T obj = (T)Waher.Content.JSON.Parse(existingState.Value);
                    return Task.FromResult(obj);
                }
            }

            return Task.FromResult(defaultValueIfNotFound);
        }

        public Task RemoveState(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.CompletedTask;

            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                connection.Delete<Setting>(key);
            }

            return Task.CompletedTask;
        }

        public Task RemoveStateWhere(Func<string, bool> predicate)
        {
            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                List<Setting> existingStates = connection.Table<Setting>().ToList();
                foreach (var state in existingStates)
                {
                    if (predicate(state.Key))
                    {
                        connection.Delete<Setting>(state.Key);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}