using IdApp.Pages.Contacts.Chat.MarkdownExtensions.Content;
using IdApp.Services.AttachmentCache;
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
	/// A <see cref="MultimediaContent"/> implementation which renders markdown images in Xamarin.Forms using FFImageLoading library.
	/// </summary>
	/// <remarks>
	/// Rendering for destinations other than Xamarin.Forms is performed using the default <see cref="ImageContent"/>.
	/// </remarks>
	public class FFImageMultimediaContent : MultimediaContent, IMultimediaXamarinFormsXamlRenderer
	{
		private readonly ImageContent imageContent = new();

		/// <inheritdoc/>
		public override Grade Supports(MultimediaItem Item)
		{
			if (Item.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
				return Grade.Excellent;

			if (Item.Url?.StartsWith(Constants.UriSchemes.Aes256, StringComparison.OrdinalIgnoreCase) == true)
			{
				if (Aes256Getter.TryParse(new Uri(Item.Url), out _, out _, out string ContentType, out _))
				{
					if (ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
						return Grade.Excellent;
				}
			}

			return this.imageContent.Supports(Item);
		}

		/// <inheritdoc/>
		public override bool EmbedInlineLink(string Url)
		{
			return this.imageContent.EmbedInlineLink(Url);
		}

		/// <inheritdoc/>
		public Task RenderXamarinFormsXaml(XamarinFormsXamlRenderer Renderer, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			if (Items?.Length > 0 && Items[0] is MultimediaItem MultimediaItem)
			{
				XmlWriter Output = Renderer.XmlOutput;
				double? DownsampleWidth = null;
				double? DownsampleHeight = null;

				// FFImageLoading preserves aspect ratio, so we only need to compute which dimension to specify explicitly.
				if (MultimediaItem.Width.HasValue)
				{
					if (MultimediaItem.Height.HasValue)
					{
						if (MultimediaItem.Width >= MultimediaItem.Height && MultimediaItem.Width > Constants.MaxRenderedImageDimensionInPixels)
							DownsampleWidth = Constants.MaxRenderedImageDimensionInPixels;

						if (MultimediaItem.Height >= MultimediaItem.Width && MultimediaItem.Height > Constants.MaxRenderedImageDimensionInPixels)
							DownsampleHeight = Constants.MaxRenderedImageDimensionInPixels;
					}
					else
					{
						if (MultimediaItem.Width > Constants.MaxRenderedImageDimensionInPixels)
							DownsampleWidth = Constants.MaxRenderedImageDimensionInPixels;
					}
				}

				string Url = MultimediaItem.Url;

				if (Url.StartsWith(Constants.UriSchemes.Aes256))
				{
					Output.WriteStartElement("ImageHelpers", "MyCachedImage", "clr-namespace:IdApp.Helpers;assembly=IdApp");
					Output.WriteAttributeString("Source", Url);

					if (DownsampleWidth.HasValue)
						Output.WriteAttributeString("DownsampleWidth", DownsampleWidth.Value.ToString());

					if (DownsampleHeight.HasValue)
						Output.WriteAttributeString("DownsampleHeight", DownsampleHeight.Value.ToString());

					// For some reason, specifying an SVG image source doesn't work, no idea why.
					Output.WriteAttributeString("LoadingPlaceholder", $"resource://{Resx.Pngs.Image}");
					Output.WriteAttributeString("ErrorPlaceholder", $"resource://{Resx.Pngs.BrokenImage}");

					Output.WriteEndElement();

					/*
					Output.WriteStartElement("Image", "http://xamarin.com/schemas/2014/forms");
					Output.WriteStartElement("Image.Source", "http://xamarin.com/schemas/2014/forms");
					Output.WriteStartElement("imagehelpers", "AesImageSource", "clr-namespace:IdApp.Helpers;assembly=IdApp");
					Output.WriteAttributeString("Uri", Url);
					Output.WriteEndElement();
					Output.WriteEndElement();
					Output.WriteEndElement();
					*/
				}
				else
				{
					Output.WriteStartElement("ffimageloading", "CachedImage", "clr-namespace:FFImageLoading.Forms;assembly=FFImageLoading.Forms");
					Output.WriteAttributeString("Source", Url);

					if (DownsampleWidth.HasValue)
						Output.WriteAttributeString("DownsampleWidth", DownsampleWidth.Value.ToString());

					if (DownsampleHeight.HasValue)
						Output.WriteAttributeString("DownsampleHeight", DownsampleHeight.Value.ToString());

					// For some reason, specifying an SVG image source doesn't work, no idea why.
					Output.WriteAttributeString("LoadingPlaceholder", $"resource://{Resx.Pngs.Image}");
					Output.WriteAttributeString("ErrorPlaceholder", $"resource://{Resx.Pngs.BrokenImage}");

					Output.WriteEndElement();
				}
			}

			return Task.CompletedTask;
		}

		private IAttachmentCacheService attachmentCacheService;

		/// <summary>
		/// Provides a reference to the attachment cache service.
		/// </summary>
		public IAttachmentCacheService AttachmentCacheService
		{
			get
			{
				this.attachmentCacheService ??= App.Instantiate<IAttachmentCacheService>();

				return this.attachmentCacheService;
			}
		}
	}
}
