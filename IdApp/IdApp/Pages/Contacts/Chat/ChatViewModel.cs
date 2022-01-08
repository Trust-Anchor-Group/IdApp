using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.Forms;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.UI;
using IdApp.Services.Messages;
using IdApp.Services.Tag;

namespace IdApp.Pages.Contacts.Chat
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class ChatViewModel : NeuronViewModel
	{
		private const int MessageBatchSize = 50;

		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates an instance of the <see cref="ContactListViewModel"/> class.
		/// </summary>
		public ChatViewModel()
			: this(null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ContactListViewModel"/> class.
		/// For unit tests.
		/// </summary>
		/// <param name="NeuronService">The Neuron service for XMPP communication.</param>
		/// <param name="UiSerializer">The dispatcher to use for alerts and accessing the main thread.</param>
		/// <param name="TagProfile">TAG Profie service.</param>
		/// <param name="NavigationService">Navigation service. </param>
		protected internal ChatViewModel(INeuronService NeuronService, IUiSerializer UiSerializer, ITagProfile TagProfile,
			INavigationService NavigationService)
			: base(NeuronService, UiSerializer, TagProfile)
		{
			this.navigationService = NavigationService ?? App.Instantiate<INavigationService>();

			this.Messages = new ObservableCollection<ChatMessage>();

			this.SendCommand = new Command(async _ => await this.ExecuteSendMessage(), _ => this.CanExecuteSendMessage());
			this.LoadMoreMessages = new Command(async _ => await this.ExecuteLoadMoreMessages(), _ => this.CanExecuteLoadMoreMessages());
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out ChatNavigationArgs args))
			{
				this.BareJid = args.BareJid;
				this.FriendlyName = args.FriendlyName;
			}
			else
			{
				this.BareJid = string.Empty;
				this.FriendlyName = string.Empty;
			}

			IEnumerable<ChatMessage> Messages = await Database.Find<ChatMessage>(0, MessageBatchSize, new FilterFieldEqualTo("RemoteBareJid", this.BareJid), "-Created");

			int c = MessageBatchSize;
			this.Messages.Clear();
			foreach (ChatMessage Message in Messages)
			{
				this.Messages.Add(Message);
				c--;
			}

			this.ExistsMoreMessages = c <= 0;

			this.EvaluateAllCommands();
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.Messages.Clear();
			await base.DoUnbind();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.SendCommand, this.LoadMoreMessages);
		}

		/// <summary>
		/// <see cref="BareJid"/>
		/// </summary>
		public static readonly BindableProperty BareJidProperty =
			BindableProperty.Create("BareJid", typeof(string), typeof(ContactListViewModel), default(string));

		/// <summary>
		/// Bare JID of remote party
		/// </summary>
		public string BareJid
		{
			get { return (string)GetValue(BareJidProperty); }
			set { SetValue(BareJidProperty, value); }
		}

		/// <summary>
		/// <see cref="FriendlyName"/>
		/// </summary>
		public static readonly BindableProperty FriendlyNameProperty =
			BindableProperty.Create("FriendlyName", typeof(string), typeof(ContactListViewModel), default(string));

		/// <summary>
		/// Friendly name of remote party
		/// </summary>
		public string FriendlyName
		{
			get { return (string)GetValue(FriendlyNameProperty); }
			set { SetValue(FriendlyNameProperty, value); }
		}

		/// <summary>
		/// <see cref="MarkdownInput"/>
		/// </summary>
		public static readonly BindableProperty MarkdownInputProperty =
			BindableProperty.Create("MarkdownInput", typeof(string), typeof(ContactListViewModel), default(string));

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		public string MarkdownInput
		{
			get { return (string)GetValue(MarkdownInputProperty); }
			set
			{
				SetValue(MarkdownInputProperty, value);
				this.IsWriting = !string.IsNullOrEmpty(value);
				this.EvaluateCommands(this.SendCommand);
			}
		}

		/// <summary>
		/// <see cref="ExistsMoreMessages"/>
		/// </summary>
		public static readonly BindableProperty ExistsMoreMessagesProperty =
			BindableProperty.Create("ExistsMoreMessages", typeof(bool), typeof(ContactListViewModel), default(bool));

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		public bool ExistsMoreMessages
		{
			get { return (bool)GetValue(ExistsMoreMessagesProperty); }
			set
			{
				SetValue(ExistsMoreMessagesProperty, value);
				this.EvaluateCommands(this.LoadMoreMessages);
			}
		}

		/// <summary>
		/// <see cref="IsWriting"/>
		/// </summary>
		public static readonly BindableProperty IsWritingProperty =
			BindableProperty.Create("IsWriting", typeof(bool), typeof(ContactListViewModel), default(bool));

		/// <summary>
		/// If the user is writing markdown.
		/// </summary>
		public bool IsWriting
		{
			get { return (bool)GetValue(IsWritingProperty); }
			set { SetValue(IsWritingProperty, value); }
		}

		/// <summary>
		/// Holds the list of chat messages to display.
		/// </summary>
		public ObservableCollection<ChatMessage> Messages { get; }

		/// <summary>
		/// See <see cref="SelectedMessage"/>
		/// </summary>
		public static readonly BindableProperty SelectedMessageProperty =
			BindableProperty.Create("SelectedMessage", typeof(ChatMessage), typeof(ContactListViewModel), default(ChatMessage),
				propertyChanged: (b, oldValue, newValue) =>
				{
					if (b is ChatViewModel viewModel && newValue is ChatMessage Message)
					{
					}
				});

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		public ChatMessage SelectedMessage
		{
			get { return (ChatMessage)GetValue(SelectedMessageProperty); }
			set { SetValue(SelectedMessageProperty, value); }
		}

		/// <summary>
		/// External message has been received
		/// </summary>
		/// <param name="Message">Message</param>
		public void MessageAdded(ChatMessage Message)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.Messages.Insert(0, Message);
			});
		}

		/// <summary>
		/// External message has been updated
		/// </summary>
		/// <param name="Message">Message</param>
		public void MessageUpdated(ChatMessage Message)
		{
			int i = 0;

			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				foreach (ChatMessage Msg in this.Messages)
				{
					if (Msg.ObjectId == Message.ObjectId)
					{
						this.Messages[i] = Message;
						break;
					}

					i++;
				}
			});
		}

		/// <summary>
		/// The command to bind to for sending user input
		/// </summary>
		public ICommand SendCommand { get; }

		private bool CanExecuteSendMessage()
		{
			return this.IsConnected && !string.IsNullOrEmpty(this.MarkdownInput);
		}

		private Task ExecuteSendMessage()
		{
			return this.ExecuteSendMessage(string.Empty);
		}

		private async Task ExecuteSendMessage(string ReplaceObjectId)
		{
			try
			{
				if (string.IsNullOrEmpty(this.MarkdownInput))
					return;

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

				MarkdownDocument Doc = await MarkdownDocument.CreateAsync(this.MarkdownInput, Settings);

				ChatMessage Message = new ChatMessage()
				{
					Created = DateTime.UtcNow,
					RemoteBareJid = this.BareJid,
					RemoteObjectId = string.Empty,
					MessageType = MessageType.Sent,
					Html = HtmlDocument.GetBody(await Doc.GenerateHTML()),
					PlainText = (await Doc.GeneratePlainText()).Trim(),
					Markdown = this.MarkdownInput,
					Xaml = await Doc.GenerateXamarinForms()
				};

				StringBuilder Xml = new StringBuilder();

				Xml.Append("<content xmlns=\"urn:xmpp:content\" type=\"text/markdown\">");
				Xml.Append(XML.Encode(this.MarkdownInput));
				Xml.Append("</content><html xmlns='http://jabber.org/protocol/xhtml-im'><body xmlns='http://www.w3.org/1999/xhtml'>");

				HtmlDocument HtmlDoc = new HtmlDocument("<root>" + Message.Html + "</root>");

				foreach (HtmlNode N in (HtmlDoc.Body ?? HtmlDoc.Root).Children)
					N.Export(Xml);

				Xml.Append("</body></html>");

				if (!string.IsNullOrEmpty(ReplaceObjectId))
				{
					Xml.Append("<replace id='");
					Xml.Append(ReplaceObjectId);
					Xml.Append("' xmlns='urn:xmpp:message-correct:0'/>");
				}

				this.NeuronService.Xmpp.SendMessage(Waher.Networking.XMPP.MessageType.Chat, this.BareJid, Xml.ToString(),
					Message.PlainText, string.Empty, string.Empty, string.Empty, string.Empty); // TODO: End-to-End encryption

				if (string.IsNullOrEmpty(ReplaceObjectId))
				{
					await Database.Insert(Message);
					this.MessageAdded(Message);
				}
				else
				{
					ChatMessage Old = await Database.TryLoadObject<ChatMessage>(ReplaceObjectId);

					if (Old is null)
					{
						ReplaceObjectId = null;
						await Database.Insert(Message);

						this.MessageAdded(Message);
					}
					else
					{
						Old.Updated = Message.Created;
						Old.Html = Message.Html;
						Old.PlainText = Message.PlainText;
						Old.Markdown = Message.Markdown;
						Old.Xaml = Message.Xaml;

						await Database.Update(Old);

						Message = Old;

						this.MessageUpdated(Message);
					}
				}

				this.MarkdownInput = string.Empty;
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// The command to bind to for loading more messages.
		/// </summary>
		public ICommand LoadMoreMessages { get; }

		private bool CanExecuteLoadMoreMessages()
		{
			return this.ExistsMoreMessages && this.Messages.Count > 0;
		}

		private async Task ExecuteLoadMoreMessages()
		{
			ChatMessage Last = this.Messages[this.Messages.Count - 1];
			IEnumerable<ChatMessage> Messages = await Database.Find<ChatMessage>(0, MessageBatchSize, new FilterAnd(
				new FilterFieldEqualTo("RemoteBareJid", this.BareJid),
				new FilterFieldLesserThan("Created", Last.Created)), "-Created");

			int c = MessageBatchSize;
			foreach (ChatMessage Message in Messages)
			{
				this.Messages.Add(Message);
				c--;
			}

			this.ExistsMoreMessages = c <= 0;
		}

	}
}