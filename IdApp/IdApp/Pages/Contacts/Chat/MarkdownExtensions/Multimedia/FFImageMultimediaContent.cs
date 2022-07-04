using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.Multimedia;
using Waher.Runtime.Inventory;

namespace IdApp.Pages.Contacts.Chat.MarkdownExtensions.Multimedia
{
	/// <summary>
	/// A <see cref="MultimediaContent"/> implementation which renders markdown images in Xamarin.Forms using FFImageLoading library.
	/// </summary>
	/// <remarks>
	/// Rendering for destinations other than Xamarin.Forms is performed using the default <see cref="ImageContent"/>.
	/// </remarks>
	public class FFImageMultimediaContent : MultimediaContent
	{
		private readonly ImageContent imageContent = new();

		/// <inheritdoc/>
		public override Grade Supports(MultimediaItem Item)
		{
			if (Item.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
			{
				return Grade.Excellent;
			}

			return this.imageContent.Supports(Item);
		}

		/// <inheritdoc/>
		public override bool EmbedInlineLink(string Url)
		{
			return this.imageContent.EmbedInlineLink(Url);
		}

		/// <inheritdoc/>
		public override Task GenerateHTML(StringBuilder Output, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			return this.imageContent.GenerateHTML(Output, Items, ChildNodes, AloneInParagraph, Document);
		}

		/// <inheritdoc/>
		public override Task GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			if (Items?.Length > 0 && Items[0] is MultimediaItem MultimediaItem)
			{
				double? DownsampleWidth = null;
				double? DownsampleHeight = null;

				// FFImageLoading preserves aspect ratio, so we only need to compute which dimension to specify explicitly.
				if (MultimediaItem.Width.HasValue)
				{
					if (MultimediaItem.Height.HasValue)
					{
						if (MultimediaItem.Width >= MultimediaItem.Height && MultimediaItem.Width > Constants.MaxRenderedImageDimensionInPixels)
						{
							DownsampleWidth = Constants.MaxRenderedImageDimensionInPixels;
						}

						if (MultimediaItem.Height >= MultimediaItem.Width && MultimediaItem.Height > Constants.MaxRenderedImageDimensionInPixels)
						{
							DownsampleHeight = Constants.MaxRenderedImageDimensionInPixels;
						}
					}
					else
					{
						if (MultimediaItem.Width > Constants.MaxRenderedImageDimensionInPixels)
						{
							DownsampleWidth = Constants.MaxRenderedImageDimensionInPixels;
						}
					}
				}

				Output.WriteStartElement("ffimageloading", "CachedImage", "clr-namespace:FFImageLoading.Forms;assembly=FFImageLoading.Forms");
				Output.WriteAttributeString("Source", MultimediaItem.Url);

				if (DownsampleWidth.HasValue)
				{
					Output.WriteAttributeString("DownsampleWidth", DownsampleWidth.Value.ToString());
				}

				if (DownsampleHeight.HasValue)
				{
					Output.WriteAttributeString("DownsampleHeight", DownsampleHeight.Value.ToString());
				}

				// For some reason, specifying an SVG image source doesn't work, no idea why.
				Output.WriteAttributeString("LoadingPlaceholder", $"resource://{Resx.Pngs.Image}");
				Output.WriteAttributeString("ErrorPlaceholder", $"resource://{Resx.Pngs.BrokenImage}");

				Output.WriteEndElement();

				return Task.CompletedTask;
			}

			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public override Task GenerateXAML(XmlWriter Output, TextAlignment TextAlignment, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			return this.imageContent.GenerateXAML(Output, TextAlignment, Items, ChildNodes, AloneInParagraph, Document);
		}
	}
}
