using System.Collections.Generic;
using System.Linq;

namespace Tag.Neuron.Xamarin.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/> objects.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Creates an <see cref="IEnumerable{TSource}"/> from the specified item.
        /// </summary>
        /// <typeparam name="TSource">The desired type.</typeparam>
        /// <param name="item">The item to convert to an IEnumerable.</param>
        /// <returns></returns>
        public static IEnumerable<TSource> Create<TSource>(TSource item)
        {
            return new[] { item };
        }

        /// <summary>
        /// Appends an item to an existing <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The item type</typeparam>
        /// <param name="first">The enumerable to append to.</param>
        /// <param name="second">The item to append.</param>
        /// <returns></returns>
        public static IEnumerable<TSource> Append<TSource>(IEnumerable<TSource> first, TSource second)
        {
            return first.Concat(Create(second));
        }
    }
}