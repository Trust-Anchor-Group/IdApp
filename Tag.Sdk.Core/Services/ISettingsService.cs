using System;
using System.Collections.Generic;

namespace Tag.Sdk.Core.Services
{
    public interface ISettingsService
    {
        void SaveState(string key, object state);
        T RestoreState<T>(string key, T defaultValueIfNotFound = default);
        IEnumerable<(string key, T value)> RestoreStateWhere<T>(Func<string, bool> predicate);
        void RemoveState(string key);
        void RemoveStateWhere(Func<string, bool> predicate);
    }
}