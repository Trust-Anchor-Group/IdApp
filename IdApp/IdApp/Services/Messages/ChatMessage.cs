using IdApp.Pages.Contacts.Chat;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content.Markdown;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Services.Messages
{
	/// <summary>
	/// Message type
	/// </summary>
	public enum MessageType
	{
		/// <summary>
		/// Message sent by the user
		/// </summary>
		Sent,

		/// <summary>
		/// Message received from remote party
		/// </summary>
		Received
	}

	/// <summary>
	/// Chat Messages
	/// </summary>
	[CollectionName("ChatMessages")]
	[TypeName(TypeNameSerialization.None)]
	[Index("RemoteBareJid", "Created")]
	[Index("RemoteBareJid", "RemoteObjectId")]
	public class ChatMessage
	{
		private string objectId = null;
		private CaseInsensitiveString remoteBareJid = null;
		private DateTime created = DateTime.MinValue;
		private DateTime updated = DateTime.MinValue;
		private string remoteObjectId = null;
		private MessageType messageType = MessageType.Sent;
		private string plainText = string.Empty;
		private string markdown = string.Empty;
		private string html = string.Empty;
		private object parsedXaml = null;

		private IChatView chatView;

		/// <summary>
		/// Chat Messages
		/// </summary>
		public ChatMessage()
		{
			this.Updated = DateTime.MinValue;
	
			this.XmppUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.Xmpp));
			this.IotIdUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.IotId));
			this.IotScUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.IotSc));
			this.IotDiscoUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.IotDisco));
			this.EDalerUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.EDaler));
		}

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string ObjectId
		{
			get => this.objectId;
			set => this.objectId = value;
		}

		/// <summary>
		/// Remote Bare JID
		/// </summary>
		public CaseInsensitiveString RemoteBareJid
		{
			get => this.remoteBareJid;
			set => this.remoteBareJid = value;
		}

		/// <summary>
		/// When message was created
		/// </summary>
		public DateTime Created
		{
			get => this.created;
			set => this.created = value;
		}

		/// <summary>
		/// When message was created
		/// </summary>
		[DefaultValueDateTimeMinValue]
		public DateTime Updated
		{
			get => this.updated;
			set => this.updated = value;
		}

		/// <summary>
		/// Remote Objcet ID. If sent by the local user, value will be null or empty.
		/// </summary>
		public string RemoteObjectId
		{
			get => this.remoteObjectId;
			set => this.remoteObjectId = value;
		}

		/// <summary>
		/// Message Type
		/// </summary>
		public MessageType MessageType
		{
			get => this.messageType;
			set => this.messageType = value;
		}

		/// <summary>
		/// Plain text of message
		/// </summary>
		public string PlainText
		{
			get => this.plainText;
			set => this.plainText = value;
		}

		/// <summary>
		/// Markdown of message
		/// </summary>
		public string Markdown
		{
			get => this.markdown;
			set => this.markdown = value;
		}

		/// <summary>
		/// HTML of message
		/// </summary>
		public string Html
		{
			get => this.html;
			set
			{
				this.html = value;
				this.parsedXaml = null;
			}
		}

		/// <summary>
		/// Message Style ID
		/// </summary>
		public string StyleId => "Message" + this.messageType.ToString();

		/// <summary>
		/// Parses the XAML in the message.
		/// </summary>
		/// <param name="View"></param>
		public async Task GenerateXaml(IChatView View)
		{
			this.chatView = View;

			if (!string.IsNullOrEmpty(this.markdown))
			{
				try
				{
					MarkdownSettings Settings = new MarkdownSettings()
					{
						AllowScriptTag = false,
						EmbedEmojis = false,    // TODO: Emojis
						AudioAutoplay = false,
						AudioControls = false,
						ParseMetaData = false,
						VideoAutoplay = false,
						VideoControls = false
					};

					MarkdownDocument Doc = await MarkdownDocument.CreateAsync(this.markdown, Settings);

					string Xaml = await Doc.GenerateXamarinForms();

					this.parsedXaml = new StackLayout().LoadFromXaml(Xaml);

					if (this.parsedXaml is StackLayout Layout)
						Layout.StyleId = this.StyleId;
				}
				catch (Exception ex)
				{
					Log.Critical(ex);

					StackLayout Layout = new StackLayout()
					{
						Orientation = StackOrientation.Vertical,
						StyleId = this.StyleId
					};

					Layout.Children.Add(new Label()
					{
						Text = ex.Message,
						FontFamily = "Courier New",
						TextColor = Color.Red,
						TextType = TextType.Text
					});

					this.parsedXaml = Layout;
				}
			}
			else
			{
				StackLayout Layout = new StackLayout()
				{
					Orientation = StackOrientation.Vertical,
					StyleId = this.StyleId
				};

				if (!string.IsNullOrEmpty(this.html))
				{
					Layout.Children.Add(new Label()
					{
						Text = this.html,
						TextType = TextType.Html
					});
				}
				else if (!string.IsNullOrEmpty(this.plainText))
				{
					Layout.Children.Add(new Label()
					{
						Text = this.plainText,
						TextType = TextType.Text
					});
				}

				this.parsedXaml = Layout;
			}
		}

		/// <summary>
		/// Parsed XAML
		/// </summary>
		public object ParsedXaml => this.parsedXaml;

		/// <summary>
		/// Command executed when a multi-media-link with the xmpp URI scheme is clicked.
		/// </summary>
		public ICommand XmppUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotid URI scheme is clicked.
		/// </summary>
		public ICommand IotIdUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotsc URI scheme is clicked.
		/// </summary>
		public ICommand IotScUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotdisco URI scheme is clicked.
		/// </summary>
		public ICommand IotDiscoUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the edaler URI scheme is clicked.
		/// </summary>
		public ICommand EDalerUriClicked { get; }

		private Task ExecuteUriClicked(object Parameter, UriScheme Scheme)
		{
			if (Parameter is string Uri && !(this.chatView is null))
				return this.chatView.ExecuteUriClicked(this, Uri, Scheme);
			else
				return Task.CompletedTask;
		}

	}
}
