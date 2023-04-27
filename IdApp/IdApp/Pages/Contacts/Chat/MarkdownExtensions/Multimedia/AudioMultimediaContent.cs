﻿using IdApp.Pages.Contacts.Chat.MarkdownExtensions.Content;
using IdApp.Services.AttachmentCache;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.Multimedia;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Contacts.Chat.MarkdownExtensions.Multimedia
{
	/// <summary>
	/// A <see cref="MultimediaContent"/> implementation which renders markdown images in Xamarin.Forms using FFImageLoading library.
	/// </summary>
	/// <remarks>
	/// Rendering for destinations other than Xamarin.Forms is performed using the default <see cref="ImageContent"/>.
	/// </remarks>
	public class AudioMultimediaContent : MultimediaContent
	{
		private readonly AudioContent audioContent = new();

		/// <inheritdoc/>
		public override Grade Supports(MultimediaItem Item)
		{
			if (Item.ContentType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) == true)
				return Grade.Excellent;

			if (Item.Url?.StartsWith(Constants.UriSchemes.Aes256, StringComparison.OrdinalIgnoreCase) == true)
			{
				if (Aes256Getter.TryParse(new Uri(Item.Url), out _, out _, out string ContentType, out Uri EncriptedUri))
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
		public override Task GenerateHTML(StringBuilder Output, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			return this.audioContent.GenerateHTML(Output, Items, ChildNodes, AloneInParagraph, Document);
		}

		/// <inheritdoc/>
		public override Task GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			if (Items?.Length > 0 && Items[0] is MultimediaItem MultimediaItem)
			{
				string Url = MultimediaItem.Url;

				if (Url.StartsWith(Constants.UriSchemes.Aes256))
				{
					Output.WriteStartElement("toolkit", "MediaElement", "http://xamarin.com/schemas/2020/toolkit");
					Output.WriteAttributeString("Source", "https://www2.cs.uic.edu/~i101/SoundFiles/BabyElephantWalk60.wav");
					Output.WriteAttributeString("HorizontalOptions", "FillAndExpand");
					Output.WriteAttributeString("HeightRequest", "100");
					Output.WriteAttributeString("WidthRequest", "300");
					Output.WriteAttributeString("AutoPlay", "False");
					Output.WriteAttributeString("ShowsPlaybackControls", "True");
					Output.WriteAttributeString("KeepScreenOn", "True");
					Output.WriteEndElement();
				}
				else
				{
					Output.WriteStartElement("ffimageloading", "CachedImage", "clr-namespace:FFImageLoading.Forms;assembly=FFImageLoading.Forms");
					Output.WriteAttributeString("Source", Url);
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

		/// <inheritdoc/>
		public override Task GenerateXAML(XmlWriter Output, TextAlignment TextAlignment, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			return this.audioContent.GenerateXAML(Output, TextAlignment, Items, ChildNodes, AloneInParagraph, Document);
		}

		/// <inheritdoc/>
		public override Task GenerateLaTeX(StringBuilder Output, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			return this.audioContent.GenerateLaTeX(Output, Items, ChildNodes, AloneInParagraph, Document);
		}
	}
}