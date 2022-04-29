using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IdApp.Extensions
{
	/// <summary>
	/// An extensions class for the <see cref="string"/> class.
	/// </summary>
	public static class StringExtensions
	{
		private static readonly List<string> InvalidFileNameChars;
		private static readonly List<string> InvalidPathChars;

		static StringExtensions()
		{
			InvalidFileNameChars = Path.GetInvalidFileNameChars().Select(x => x.ToString()).ToList();
			InvalidPathChars = Path.GetInvalidPathChars().Select(x => x.ToString()).ToList();
		}

		/// <summary>
		/// Does a best effort of converting any given string to a valid file name.
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <returns>A valid file name, or <c>null</c>.</returns>
		public static string ToSafeFileName(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
				return string.Empty;

			str = str.Trim().Replace(" ", string.Empty);

			foreach (string s in InvalidFileNameChars)
			{
				str = str.Replace(s, string.Empty);
			}

			foreach (string s in InvalidPathChars)
			{
				str = str.Replace(s, string.Empty);
			}

			return str;
		}

		/// <summary>
		/// Returns the part of the string that appears before <paramref name="Delimiter"/>. If <paramref name="Delimiter"/>
		/// does not occur, the entire string is returned.
		/// </summary>
		/// <param name="s">String</param>
		/// <param name="Delimiter">Delimiter</param>
		/// <returns>Part of string before <paramref name="Delimiter"/>.</returns>
		public static string Before(this string s, string Delimiter)
		{
			int i = s.IndexOf(Delimiter);
			if (i < 0)
				return s;
			else
				return s.Substring(0, i);
		}

		/// <summary>
		/// Returns the part of the string that appears before <paramref name="Delimiter"/>. If <paramref name="Delimiter"/>
		/// does not occur, the entire string is returned.
		/// </summary>
		/// <param name="s">String</param>
		/// <param name="Delimiter">Delimiter</param>
		/// <returns>Part of string before <paramref name="Delimiter"/>.</returns>
		public static string After(this string s, string Delimiter)
		{
			int i = s.IndexOf(Delimiter);
			if (i < 0)
				return s;
			else
				return s[(i + 1)..];
		}
	}
}