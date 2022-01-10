using IdApp.Pages.Contracts.MyContracts.ObjectModel;
using IdApp.Services;
using IdApp.Services.AttachmentCache;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using SkiaSharp;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.Multimedia;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Pages.Contacts.Chat.MarkdownExtensions.CodeBlocks
{
	/// <summary>
	/// Handles embedded Smart Contracts.
	/// </summary>
	public class IoTScCodeBlock : ICodeContent
	{
		private MarkdownDocument document;
		private Contract contract;

		/// <summary>
		/// Handles embedded Smart Contracts.
		/// </summary>
		public IoTScCodeBlock()
		{
		}

		/// <summary>
		/// If generation of HTML is supported
		/// </summary>
		public bool HandlesHTML => false;

		/// <summary>
		/// If generation of plain text is supported.
		/// </summary>
		public bool HandlesPlainText => false;

		/// <summary>
		/// If generation of XAML is supported.
		/// </summary>
		public bool HandlesXAML => true;

		/// <summary>
		/// Generates HTML (not supported)
		/// </summary>
		public Task<bool> GenerateHTML(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult<bool>(false);
		}

		/// <summary>
		/// Generates Plain Text (not supported)
		/// </summary>
		public Task<bool> GeneratePlainText(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult<bool>(false);
		}

		/// <summary>
		/// Generates WPF XAML (not supported)
		/// </summary>
		public Task<bool> GenerateXAML(XmlWriter Output, TextAlignment TextAlignment, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult<bool>(false);
		}

		/// <summary>
		/// Generates Xamarin XAML
		/// </summary>
		public async Task<bool> GenerateXamarinForms(XmlWriter Output, TextAlignment TextAlignment, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			if (this.contract is null)
			{
				try
				{
					StringBuilder sb = new StringBuilder();

					foreach (string Row in Rows)
						sb.AppendLine(Row);

					XmlDocument Doc = new XmlDocument();
					Doc.LoadXml(sb.ToString());

					ParsedContract Parsed = await Contract.Parse(Doc.DocumentElement);
					this.contract = Parsed.Contract;
				}
				catch (Exception ex)
				{
					Output.WriteStartElement("Label");
					Output.WriteAttributeString("Text", ex.Message);
					Output.WriteAttributeString("FontFamily", "Courier New");
					Output.WriteAttributeString("TextColor", "Red");
					Output.WriteEndElement();

					return false;
				}
			}

			Output.WriteStartElement("StackLayout");
			Output.WriteAttributeString("Orientation", "Vertical");
			Output.WriteAttributeString("HorizontalOptions", "Center");

			bool ImageShown = false;

			if (!(this.contract.Attachments is null))
			{
				string ImageUrl = null;

				foreach (Attachment Attachment in this.contract.Attachments)
				{
					if (Attachment.ContentType.StartsWith("image/"))
					{
						if (Attachment.ContentType == "image/png")
						{
							ImageUrl = Attachment.Url;
							break;
						}
						else if (ImageUrl is null)
							ImageUrl = Attachment.Url;
					}
				}

				if (!string.IsNullOrEmpty(ImageUrl))
				{
					IAttachmentCacheService Attachments = App.Instantiate<IAttachmentCacheService>();

					(byte[] Data, string _) = await Attachments.TryGet(ImageUrl);

					if (!(Data is null))
					{
						string FileName = await ImageContent.GetTemporaryFile(Data);
						int Width;
						int Height;

						using (SKBitmap Bitmap = SKBitmap.Decode(Data))
						{
							Width = Bitmap.Width;
							Height = Bitmap.Height;
						}

						double ScaleWidth = 300.0 / Width;
						double ScaleHeight = 300.0 / Height;
						double Scale = Math.Min(ScaleWidth, ScaleHeight);

						if (Scale < 1)
						{
							Width = (int)(Width * Scale + 0.5);
							Height = (int)(Height * Scale + 0.5);
						}

						Output.WriteStartElement("Image");
						Output.WriteAttributeString("Source", FileName);
						Output.WriteAttributeString("WidthRequest", Width.ToString());
						Output.WriteAttributeString("HeightRequest", Height.ToString());
						Output.WriteEndElement();

						ImageShown = true;
					}
				}
			}

			if (!ImageShown)
			{
				Output.WriteStartElement("Label");
				Output.WriteAttributeString("Text", FontAwesome.Paragraph);
				Output.WriteAttributeString("FontFamily", "{StaticResource FontAwesomeSolid}");
				Output.WriteAttributeString("FontSize", "Large");
				Output.WriteAttributeString("HorizontalOptions", "Center");
				Output.WriteEndElement();
			}

			string FriendlyName = await ContractModel.GetCategory(this.contract);

			Output.WriteStartElement("Label");
			Output.WriteAttributeString("LineBreakMode", "WordWrap");
			Output.WriteAttributeString("FontSize", "Medium");
			Output.WriteAttributeString("HorizontalOptions", "Center");
			Output.WriteAttributeString("Text", FriendlyName);
			Output.WriteEndElement();

			Output.WriteStartElement("StackLayout.GestureRecognizers");

			Output.WriteStartElement("TapGestureRecognizer");
			Output.WriteAttributeString("Command", "{Binding Path=IotScUriClicked}");
			Output.WriteAttributeString("CommandParameter", this.contract.ContractIdUriString);
			Output.WriteEndElement();

			Output.WriteEndElement();
			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Registers the Markdown document in which the construct resides.
		/// </summary>
		/// <param name="Document">Markdown document</param>
		public void Register(MarkdownDocument Document)
		{
			this.document = Document;
		}

		/// <summary>
		/// How much the module supports code of a given language (i.e. type of content)
		/// </summary>
		/// <param name="Language">Code language</param>
		/// <returns>Grade of support.</returns>
		public Grade Supports(string Language)
		{
			return string.Compare(Language, Constants.UriSchemes.UriSchemeIotSc, true) == 0 ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
