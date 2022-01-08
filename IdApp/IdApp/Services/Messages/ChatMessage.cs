using IdApp.Pages.Contacts.Chat;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
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
		private string xaml = string.Empty;
		private object parsedXaml = null;

		private IChatView chatView;

		/// <summary>
		/// Chat Messages
		/// </summary>
		public ChatMessage()
		{
			this.Updated = DateTime.MinValue;
	
			this.XmppUriClicked = new Command(async Parameter => await this.ExecuteXmppUriClicked(Parameter, UriScheme.Xmpp));
			this.IotIdUriClicked = new Command(async Parameter => await this.ExecuteXmppUriClicked(Parameter, UriScheme.IotId));
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
		/// HTML of message
		/// </summary>
		public string Xaml
		{
			get => this.xaml;
			set
			{
				this.xaml = value;
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
		public void ParseXaml(IChatView View)
		{
			this.chatView = View;

			if (!string.IsNullOrEmpty(this.xaml))
			{
				try
				{
					this.parsedXaml = new StackLayout().LoadFromXaml(this.xaml);

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
		public object ParsedXaml
		{
			get
			{
				if (this.parsedXaml is null)
					this.ParseXaml(null);

				return this.parsedXaml;
			}
		}

		/// <summary>
		/// Command executed when a multi-media-link with the xmpp URI scheme is clicked.
		/// </summary>
		public ICommand XmppUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotid URI scheme is clicked.
		/// </summary>
		public ICommand IotIdUriClicked { get; }

		private Task ExecuteXmppUriClicked(object Parameter, UriScheme Scheme)
		{
			if (Parameter is string Uri && !(this.chatView is null))
				return this.chatView.ExecuteXmppUriClicked(this, Uri, Scheme);
			else
				return Task.CompletedTask;
		}

	}
}
