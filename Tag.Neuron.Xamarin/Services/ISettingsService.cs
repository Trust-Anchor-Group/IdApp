using System;
using System.Collections.Generic;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Handles common UI settings that need to be persisted during sessions.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Saves state with the given key.
        /// </summary>
        /// <param name="key">The key to use.</param>
        /// <param name="state">The state to save.</param>
        void SaveState(string key, object state);
        /// <summary>
        /// Restores state for the specified key.
        /// </summary>
        /// <typeparam name="T">The state type.</typeparam>
        /// <param name="key">The state id.</param>
        /// <param name="defaultValueIfNotFound">The default value to use if the state isn't found.</param>
        /// <returns></returns>
        T RestoreState<T>(string key, T defaultValueIfNotFound = default);
        /// <summary>
        /// Returns any states whose key matches the specified predicate.
        /// </summary>
        /// <typeparam name="T">The state type.</typeparam>
        /// <param name="predicate">A predication function that identifies the relevant keys.</param>
        /// <returns>a list of matching states.</returns>
        IEnumerable<(string key, T value)> RestoreStateWhere<T>(Func<string, bool> predicate);
        /// <summary>
        /// Removes a given state.
        /// </summary>
        /// <param name="key">The state identifier.</param>
        void RemoveState(string key);
        /// <summary>
        /// Removes any states whose key matches the specified predicate.
        /// </summary>
        /// <param name="predicate">A predication function that identifies the relevant keys.</param>
        void RemoveStateWhere(Func<string, bool> predicate);
    }
}