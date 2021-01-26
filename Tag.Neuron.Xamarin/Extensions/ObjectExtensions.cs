using System.Collections.Generic;
using System.Reflection;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class ObjectExtensions
    {
        public static KeyValuePair<string, string>[] GetClassAndMethod(this object obj, MethodBase methodInfo, string method = null)
        {
            return new[]
            {
                new KeyValuePair<string, string>("Class", obj.GetType().Name),
                new KeyValuePair<string, string>("Class", method ?? methodInfo.Name)
            };
        }
    }
}