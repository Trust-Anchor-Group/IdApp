using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XamarinApp.Services
{
    public interface ISettingsService
    {
        Task SaveState(string key, object state);
        Task<T> RestoreState<T>(string key, T defaultValue = default(T));
        Task<IEnumerable<(string key, T value)>> RestoreStateWhere<T>(Func<string, bool> predicate);
        Task RemoveState<T>(string key);
    }
}