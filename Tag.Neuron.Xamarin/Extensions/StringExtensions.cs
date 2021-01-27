using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class StringExtensions
    {
        private static readonly List<string> invalidFileNameChars;
        private static readonly List<string> invalidPathChars;

        static StringExtensions()
        {
            invalidFileNameChars = Path.GetInvalidFileNameChars().Select(x => x.ToString()).ToList();
            invalidPathChars = Path.GetInvalidPathChars().Select(x => x.ToString()).ToList();
        }

        public static string ToSafeFileName(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return string.Empty;

            str = str.Trim().Replace(" ", string.Empty);

            foreach (string s in invalidFileNameChars)
            {
                str = str.Replace(s, string.Empty);
            }

            foreach (string s in invalidPathChars)
            {
                str = str.Replace(s, string.Empty);
            }

            return str;
        }
    }
}