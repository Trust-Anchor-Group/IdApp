﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SQLite;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.Services
{
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

        public void SaveState(string key, object state)
        {
            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                var existingState = connection.Find<Setting>(key);
                if (existingState != null)
                {
                    existingState.Value = JsonConvert.SerializeObject(state);
                    connection.Update(existingState);
                }
                else
                {
                    var newState = new Setting { Key = key, Value = JsonConvert.SerializeObject(state) };
                    connection.Insert(newState);
                }
            }
        }

        public IEnumerable<(string key, T value)> RestoreStateWhere<T>(Func<string, bool> predicate)
        {
            List<Setting> existingStates;
            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                existingStates = connection.Table<Setting>().ToList();
            }
            foreach (var state in existingStates)
            {
                if (predicate(state.Key))
                {
                    yield return (state.Key, JsonConvert.DeserializeObject<T>(state.Value));
                }
            }
        }

        public T RestoreState<T>(string key, T defaultValueIfNotFound = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValueIfNotFound;

            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                var existingState = connection.Find<Setting>(key);
                if (existingState != null)
                {
                    return JsonConvert.DeserializeObject<T>(existingState.Value);
                }
            }

            return defaultValueIfNotFound;
        }

        public void RemoveState(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            using (var connection = new SQLiteConnection(DatabasePath, Flags))
            {
                connection.Delete<Setting>(key);
            }
        }

        public void RemoveStateWhere(Func<string, bool> predicate)
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
        }
    }
}