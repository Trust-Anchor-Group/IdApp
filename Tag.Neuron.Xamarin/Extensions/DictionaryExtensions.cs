using System.Collections.Generic;

namespace Tag.Neuron.Xamarin.Extensions
{
    /// <summary>
    /// An extensions class for <see cref="Dictionary{TKey,TValue}"/> operations.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds the specified string value with the given name (key) to the dictionary. If either values are <c>null</c> or empty, nothing happens.
        /// </summary>
        /// <param name="dict">The dictionary to add to.</param>
        /// <param name="name">The name to use as  key.</param>
        /// <param name="value">The value to add.</param>
        public static void AddToDictionary(this Dictionary<string, string> dict, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
            {
                dict[name] = value;
            }
        }

    }
}