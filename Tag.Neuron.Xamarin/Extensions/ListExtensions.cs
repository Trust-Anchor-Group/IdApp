using System.Collections.Generic;
using System.Linq;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Compares to lists for content equality.
        /// </summary>
        /// <typeparam name="T">The list content type.</typeparam>
        /// <param name="list">The first list to compare.</param>
        /// <param name="other">The second list to compare.</param>
        /// <returns></returns>
        public static bool HasSameContentAs<T>(this List<T> list, List<T> other)
        {
            if (list.Count != other.Count)
                return false;

            var firstNotSecond = list.Except(other).ToList();
            var secondNotFirst = other.Except(list).ToList();
            return !firstNotSecond.Any() && !secondNotFirst.Any();
        }
    }
}