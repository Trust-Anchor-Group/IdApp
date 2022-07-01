﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Runtime.Inventory;

namespace IdApp.Pages.Contacts.Chat.MarkdownExtensions.Multimedia
{
	public class FFImageMultimediaContent : MultimediaContent
	{
		public override Grade Supports(MultimediaItem Item)
		{
			if (Item.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
			{
				return Grade.Excellent;
			}

			return Grade.NotAtAll;
		}

		public override bool EmbedInlineLink(string Url)
		{
			return true;
		}

		public override async Task GenerateHTML(StringBuilder Output, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MarkdownElement E in ChildNodes)
				await E.GenerateHTML(Output);
		}

		public override Task GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			if (Items?.Length > 0 && Items[0] is MultimediaItem MultimediaItem)
			{
				double? DownsampleWidth = null;
				double? DownsampleHeight = null;

				// FFImageLoading preserves aspect ratio, so we only need to compute one dimension.
				if (MultimediaItem.Width.HasValue)
				{
					if (MultimediaItem.Height.HasValue)
					{
						if (MultimediaItem.Width >= MultimediaItem.Height && MultimediaItem.Width > Constants.MaxRenderedImageDimension)
						{
							DownsampleWidth = Constants.MaxRenderedImageDimension;
						}

						if (MultimediaItem.Height >= MultimediaItem.Width && MultimediaItem.Height > Constants.MaxRenderedImageDimension)
						{
							DownsampleHeight = Constants.MaxRenderedImageDimension;
						}
					}
					else
					{
						if (MultimediaItem.Width > Constants.MaxRenderedImageDimension)
						{
							DownsampleWidth = Constants.MaxRenderedImageDimension;
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

				Output.WriteEndElement();
				return Task.CompletedTask;
			}

			throw new NotImplementedException();
		}

		public override Task GenerateXAML(XmlWriter Output, TextAlignment TextAlignment, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			throw new NotImplementedException();
		}
	}
}