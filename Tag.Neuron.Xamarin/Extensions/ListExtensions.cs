using System.Collections.Generic;
using System.Linq;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class ListExtensions
    {
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