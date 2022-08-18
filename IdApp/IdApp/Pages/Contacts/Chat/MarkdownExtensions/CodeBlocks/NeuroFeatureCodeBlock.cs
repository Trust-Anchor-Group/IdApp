using IdApp.Resx;
using NeuroFeatures;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Runtime.Inventory;

namespace IdApp.Pages.Contacts.Chat.MarkdownExtensions.CodeBlocks
{
	/// <summary>
	/// Handles embedded tokens.
	/// </summary>
	public class NeuroFeatureCodeBlock : ICodeContent
	{
		private MarkdownDocument document;

		/// <summary>
		/// Handles embedded tokens.
		/// </summary>
		public NeuroFeatureCodeBlock()
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
		/// Markdown document.
		/// </summary>
		public MarkdownDocument Document => this.document;

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
		public Task<bool> GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			Token Token;

			try
			{
				StringBuilder sb = new();

				foreach (string Row in Rows)
					sb.AppendLine(Row);

				XmlDocument Doc = new()
				{
					PreserveWhitespace = true
				};
				Doc.LoadXml(sb.ToString());

				if (!NeuroFeatures.Token.TryParse(Doc.DocumentElement, out Token))
					throw new Exception(AppResources.InvalidNeuroFeatureToken);
			}
			catch (Exception ex)
			{
				Output.WriteStartElement("Label");
				Output.WriteAttributeString("Text", ex.Message);
				Output.WriteAttributeString("FontFamily", "Courier New");
				Output.WriteAttributeString("TextColor", "Red");
				Output.WriteEndElement();

				return Task.FromResult<bool>(false);
			}

			Output.WriteStartElement("StackLayout");
			Output.WriteAttributeString("Orientation", "Vertical");
			Output.WriteAttributeString("HorizontalOptions", "Center");

			Output.WriteStartElement("Label");
			Output.WriteAttributeString("Text", FontAwesome.StarOfLife);
			Output.WriteAttributeString("FontFamily", "{StaticResource FontAwesomeSolid}");
			Output.WriteAttributeString("FontSize", "Large");
			Output.WriteAttributeString("HorizontalOptions", "Center");
			Output.WriteEndElement();

			Output.WriteStartElement("Label");
			Output.WriteAttributeString("LineBreakMode", "WordWrap");
			Output.WriteAttributeString("FontSize", "Medium");
			Output.WriteAttributeString("HorizontalOptions", "Center");
			Output.WriteAttributeString("Text", Token.FriendlyName);
			Output.WriteEndElement();

			Output.WriteStartElement("StackLayout.GestureRecognizers");

			Output.WriteStartElement("TapGestureRecognizer");
			Output.WriteAttributeString("Command", "{Binding Path=NeuroFeatureUriClicked}");
			Output.WriteAttributeString("CommandParameter", Constants.UriSchemes.UriSchemeNeuroFeature + ":" + Token.ToXml());
			Output.WriteEndElement();

			Output.WriteEndElement();
			Output.WriteEndElement();

			return Task.FromResult<bool>(true);
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
			return string.Compare(Language, Constants.UriSchemes.UriSchemeNeuroFeature, true) == 0 ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
