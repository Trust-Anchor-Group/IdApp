using System.Collections.Generic;
using System.Reflection;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Returns the current class and method as a <see cref="KeyValuePair{TKey,TValue}"/>. For debugging purposes.
        /// </summary>
        /// <param name="obj">The object whose class and method to extract.</param>
        /// <param name="methodInfo">The current method instance.</param>
        /// <param name="method">An optional method name. If not specified, the method name is extracted from the <c>methodInfo</c> parameter.</param>
        /// <returns></returns>
        public static KeyValuePair<string, string>[] GetClassAndMethod(this object obj, MethodBase methodInfo, string method = null)
        {
            return new[]
            {
                new KeyValuePair<string, string>("Class", obj.GetType().Name),
                new KeyValuePair<string, string>("Method", !string.IsNullOrWhiteSpace(method) ? method : methodInfo.Name)
            };
        }
    }
}