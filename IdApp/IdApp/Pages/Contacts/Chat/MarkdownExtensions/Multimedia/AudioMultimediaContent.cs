using IdApp.Pages.Contacts.Chat.MarkdownExtensions.Content;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.Multimedia;
using Waher.Content.Markdown.Xamarin;
using Waher.Runtime.Inventory;

namespace IdApp.Pages.Contacts.Chat.MarkdownExtensions.Multimedia
{
	/// <summary>
	/// A <see cref="MultimediaContent"/> implementation which plays audio using a AudioPlayerControl.
	/// </summary>
	public class AudioMultimediaContent : MultimediaContent, IMultimediaXamarinFormsXamlRenderer
	{
		private readonly AudioContent audioContent = new();

		/// <inheritdoc/>
		public override Grade Supports(MultimediaItem Item)
		{
			if (Item.ContentType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) == true)
				return Grade.Excellent;

			if (Item.Url?.StartsWith(Constants.UriSchemes.Aes256, StringComparison.OrdinalIgnoreCase) == true)
			{
				if (Aes256Getter.TryParse(new Uri(Item.Url), out _, out _, out string ContentType, out _))
				{
					if (ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) == true)
						return Grade.Excellent;
				}
			}

			return this.audioContent.Supports(Item);
		}

		/// <inheritdoc/>
		public override bool EmbedInlineLink(string Url)
		{
			return this.audioContent.EmbedInlineLink(Url);
		}

		/// <inheritdoc/>
		public Task RenderXamarinFormsXaml(XamarinFormsXamlRenderer Renderer, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			if (Items?.Length > 0 && Items[0] is MultimediaItem MultimediaItem)
			{
				XmlWriter Output = Renderer.XmlOutput;
				string Url = MultimediaItem.Url;

				if (Url.StartsWith(Constants.UriSchemes.Aes256))
				{
					Output.WriteStartElement("controls", "AudioPlayerControl", "clr-namespace:IdApp.Controls;assembly=IdApp");
					Output.WriteAttributeString("Uri", Url);
					Output.WriteAttributeString("HeightRequest", "50");
					Output.WriteAttributeString("WidthRequest", "300");
					Output.WriteEndElement();
				}
				else
				{
					// throw new NotImplementedException();
				}
			}

			return Task.CompletedTask;
		}
	}
}
