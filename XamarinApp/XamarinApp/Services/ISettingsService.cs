using System;
using System.Collections.Generic;

namespace XamarinApp.Services
{
    public interface ISettingsService
    {
        void SaveState(string key, object state);
        T RestoreState<T>(string key);
        IEnumerable<(string key, T value)> RestoreStateWhere<T>(Func<string, bool> predicate);
        void RemoveState<T>(string key);
    }
}