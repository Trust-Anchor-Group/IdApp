using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Waher.Content.Markdown;
using Waher.Events;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Extensions
{
	/// <summary>
	/// An extensions class for the <see cref="string"/> class.
	/// </summary>
	public static class StringExtensions
	{
		private static readonly List<string> invalidFileNameChars;
		private static readonly List<string> invalidPathChars;

		static StringExtensions()
		{
			invalidFileNameChars = Path.GetInvalidFileNameChars().Select(x => x.ToString()).ToList();
			invalidPathChars = Path.GetInvalidPathChars().Select(x => x.ToString()).ToList();
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

		/// <summary>
		/// Returns the number of Unicode symbols, which may be represented by one or two chars, in a string.
		/// </summary>
		public static int GetUnicodeLength(this string Str)
		{
			if (Str == null)
			{
				throw new ArgumentNullException(nameof(Str));
			}

			Str = Str.Normalize();

			int UnicodeCount = 0;
			for (int i = 0; i < Str.Length; i++)
			{
				UnicodeCount++;

				// Jump over the second surrogate char.
				if (char.IsSurrogate(Str, i))
				{
					i++;
				}
			}

			return UnicodeCount;
		}

		/// <summary>
		/// Converts Markdown text to Xamarin XAML
		/// </summary>
		/// <param name="Markdown">Markdown</param>
		/// <returns>Xamarin XAML</returns>
		public static async Task<object> MarkdownToXaml(this string Markdown)
		{
			try
			{
				MarkdownSettings Settings = new()
				{
					AllowScriptTag = false,
					EmbedEmojis = false,    // TODO: Emojis
					AudioAutoplay = false,
					AudioControls = false,
					ParseMetaData = false,
					VideoAutoplay = false,
					VideoControls = false
				};

				MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown, Settings);

				string Xaml = await Doc.GenerateXamarinForms();
				Xaml = Xaml.Replace("TextColor=\"{Binding HyperlinkColor}\"", "Style=\"{StaticResource HyperlinkColor}\"");

				return new StackLayout().LoadFromXaml(Xaml);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);

				StackLayout Layout = new()
				{
					Orientation = StackOrientation.Vertical,
				};

				Layout.Children.Add(new Label()
				{
					Text = ex.Message,
					FontFamily = "Courier New",
					TextColor = Color.Red,
					TextType = TextType.Text
				});

				return Layout;
			}
		}
	}
}
