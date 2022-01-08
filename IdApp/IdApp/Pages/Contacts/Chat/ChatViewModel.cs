using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Plugin.Media.Abstractions;
using Plugin.Media;
using Waher.Content;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Persistence;
using Waher.Persistence.Filters;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Services;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Messages;
using IdApp.Services.Tag;
using IdApp.Services.UI;

namespace IdApp.Pages.Contacts.Chat
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class ChatViewModel : NeuronViewModel
	{
		private const int MessageBatchSize = 50;

		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private TaskCompletionSource<bool> waitUntilBound = new TaskCompletionSource<bool>();

		/// <summary>
		/// Creates an instance of the <see cref="ContactListViewModel"/> class.
		/// </summary>
		public ChatViewModel()
			: this(null, null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ContactListViewModel"/> class.
		/// For unit tests.
		/// </summary>
		/// <param name="NeuronService">The Neuron service for XMPP communication.</param>
		/// <param name="UiSerializer">The dispatcher to use for alerts and accessing the main thread.</param>
		/// <param name="TagProfile">TAG Profie service.</param>
		/// <param name="NavigationService">Navigation service.</param>
		/// <param name="LogService">Log service.</param>
		protected internal ChatViewModel(INeuronService NeuronService, IUiSerializer UiSerializer, ITagProfile TagProfile,
			INavigationService NavigationService, ILogService LogService)
			: base(NeuronService, UiSerializer, TagProfile)
		{
			this.navigationService = NavigationService ?? App.Instantiate<INavigationService>();
			this.logService = LogService ?? App.Instantiate<ILogService>();

			this.Messages = new ObservableCollection<ChatMessage>();

			this.SendCommand = new Command(async _ => await this.ExecuteSendMessage(), _ => this.CanExecuteSendMessage());
			this.LoadMoreMessages = new Command(async _ => await this.ExecuteLoadMoreMessages(), _ => this.CanExecuteLoadMoreMessages());
			this.TakePhoto = new Command(async _ => await this.ExecuteTakePhoto(), _ => this.CanExecuteTakePhoto());
			this.EmbedFile = new Command(async _ => await this.ExecuteEmbedFile(), _ => this.CanExecuteEmbedFile());
			this.EmbedId = new Command(async _ => await this.ExecuteEmbedId(), _ => this.CanExecuteEmbedId());
			this.EmbedContract = new Command(async _ => await this.ExecuteEmbedContract(), _ => this.CanExecuteEmbedContract());
			this.EmbedMoney = new Command(async _ => await this.ExecuteEmbedMoney(), _ => this.CanExecuteEmbedMoney());
			this.EmbedThing = new Command(async _ => await this.ExecuteEmbedThing(), _ => this.CanExecuteEmbedThing());

			this.XmppUriClicked = new Command(async Parameter => await this.ExecuteXmppUriClicked(Parameter));
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

			this.waitUntilBound.TrySetResult(true);
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.Messages.Clear();
			await base.DoUnbind();

			this.waitUntilBound = new TaskCompletionSource<bool>();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.SendCommand, this.LoadMoreMessages, this.TakePhoto, this.EmbedFile,
				this.EmbedId, this.EmbedContract, this.EmbedMoney, this.EmbedThing);
		}

		/// <inheritdoc/>
		protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			base.NeuronService_ConnectionStateChanged(sender, e);
			this.UiSerializer.BeginInvokeOnMainThread(() => this.EvaluateAllCommands());
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
				this.EvaluateAllCommands();
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

		private async Task ExecuteSendMessage()
		{
			await this.ExecuteSendMessage(string.Empty, this.MarkdownInput);
			this.MarkdownInput = string.Empty;
		}

		private async Task ExecuteSendMessage(string ReplaceObjectId, string MarkdownInput)
		{
			try
			{
				if (string.IsNullOrEmpty(MarkdownInput))
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

				MarkdownDocument Doc = await MarkdownDocument.CreateAsync(MarkdownInput, Settings);

				ChatMessage Message = new ChatMessage()
				{
					Created = DateTime.UtcNow,
					RemoteBareJid = this.BareJid,
					RemoteObjectId = string.Empty,
					MessageType = MessageType.Sent,
					Html = HtmlDocument.GetBody(await Doc.GenerateHTML()),
					PlainText = (await Doc.GeneratePlainText()).Trim(),
					Markdown = MarkdownInput,
					Xaml = await Doc.GenerateXamarinForms()
				};

				StringBuilder Xml = new StringBuilder();

				Xml.Append("<content xmlns=\"urn:xmpp:content\" type=\"text/markdown\">");
				Xml.Append(XML.Encode(MarkdownInput));
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

		/// <summary>
		/// Command to take and send a photo
		/// </summary>
		public ICommand TakePhoto { get; }

		private bool CanExecuteTakePhoto()
		{
			return this.IsConnected && !this.IsWriting && this.NeuronService.Contracts.FileUploadIsSupported;
		}

		private async Task ExecuteTakePhoto()
		{
			if (!this.NeuronService.Contracts.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported);
				return;
			}

			if (Device.RuntimePlatform == Device.iOS)
			{
				MediaFile capturedPhoto;

				try
				{
					capturedPhoto = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
					{
						CompressionQuality = 80,
						RotateImage = false
					});
				}
				catch (Exception)
				{
					await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported);
					return;
				}

				if (!(capturedPhoto is null))
				{
					try
					{
						await this.EmbedPhoto(capturedPhoto.Path, true);
					}
					catch (Exception ex)
					{
						await this.UiSerializer.DisplayAlert(ex);
					}
				}
			}
			else
			{
				FileResult capturedPhoto;

				try
				{
					capturedPhoto = await MediaPicker.CapturePhotoAsync();
					if (capturedPhoto is null)
						return;
				}
				catch (Exception)
				{
					await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported);
					return;
				}

				if (!(capturedPhoto is null))
				{
					try
					{
						await this.EmbedPhoto(capturedPhoto.FullPath, true);
					}
					catch (Exception ex)
					{
						await this.UiSerializer.DisplayAlert(ex);
					}
				}
			}
		}

		private async Task EmbedPhoto(string FilePath, bool DeleteFile)
		{
			try
			{
				byte[] Bin = File.ReadAllBytes(FilePath);
				if (!InternetContent.TryGetContentType(Path.GetExtension(FilePath), out string ContentType))
					ContentType = "application/octet-stream";

				if (Bin.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
				{
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PhotoIsTooLarge);
					return;
				}

				// Taking or picking photos switches to another app, so ID app has to reconnect again after.
				if (!await this.NeuronService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
				{
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain));
					return;
				}

				string FileName = Path.GetFileName(FilePath);
				HttpFileUploadEventArgs Slot = await this.NeuronService.Contracts.FileUploadClient.RequestUploadSlotAsync(
					FileName, ContentType, Bin.Length);

				if (!Slot.Ok)
					throw Slot.StanzaError ?? new Exception(Slot.ErrorText);

				await Slot.PUT(Bin, ContentType, (int)Constants.Timeouts.UploadFile.TotalMilliseconds);
				await this.ExecuteSendMessage(string.Empty, "![" + MarkdownDocument.Encode(FileName) + "](" + Slot.GetUrl + ")");

				// TODO: File Transfer instead of HTTP File Upload

				if (DeleteFile)
					File.Delete(FilePath);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
				this.logService.LogException(ex);
				return;
			}
		}

		/// <summary>
		/// Command to embed a file
		/// </summary>
		public ICommand EmbedFile { get; }

		private bool CanExecuteEmbedFile()
		{
			return this.IsConnected && !this.IsWriting && this.NeuronService.Contracts.FileUploadIsSupported;
		}

		private async Task ExecuteEmbedFile()
		{
			if (!this.NeuronService.Contracts.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(AppResources.PickPhoto, AppResources.SelectingAPhotoIsNotSupported);
				return;
			}

			FileResult pickedPhoto = await MediaPicker.PickPhotoAsync();

			if (!(pickedPhoto is null))
				await this.EmbedPhoto(pickedPhoto.FullPath, false);
		}

		/// <summary>
		/// Command to embed a reference to a legal ID
		/// </summary>
		public ICommand EmbedId { get; }

		private bool CanExecuteEmbedId()
		{
			return this.IsConnected && !this.IsWriting;
		}

		private async Task ExecuteEmbedId()
		{
			TaskCompletionSource<ContactInfo> SelectedContact = new TaskCompletionSource<ContactInfo>();

			await this.navigationService.GoToAsync(nameof(MyContactsPage),
				new ContactListNavigationArgs(AppResources.SelectContactToPay, SelectedContact));

			ContactInfo Contact = await SelectedContact.Task;
			if (Contact is null)
				return;

			await this.waitUntilBound.Task;		// Wait until view is bound again.

			if (!(Contact.LegalIdentity is null))
			{
				StringBuilder Markdown = new StringBuilder();

				Markdown.AppendLine("```iotid");

				Contact.LegalIdentity.Serialize(Markdown, true, true, true, true, true, true, true);

				Markdown.AppendLine();
				Markdown.AppendLine("```");

				await this.ExecuteSendMessage(string.Empty, Markdown.ToString());
				return;
			}

			if (!string.IsNullOrEmpty(Contact.LegalId))
			{
				await this.ExecuteSendMessage(string.Empty, "![" + MarkdownDocument.Encode(Contact.FriendlyName) + "](" + ContractsClient.LegalIdUriString(Contact.LegalId) + ")");
				return;
			}

			if (!string.IsNullOrEmpty(Contact.BareJid))
			{
				await this.ExecuteSendMessage(string.Empty, "![" + MarkdownDocument.Encode(Contact.FriendlyName) + "](xmpp:" + Contact.BareJid + "?subscribe)");
				return;
			}
		}

		/// <summary>
		/// Command to embed a reference to a smart contract
		/// </summary>
		public ICommand EmbedContract { get; }

		private bool CanExecuteEmbedContract()
		{
			return this.IsConnected && !this.IsWriting;
		}

		private async Task ExecuteEmbedContract()
		{
			// TODO
		}

		/// <summary>
		/// Command to embed a payment
		/// </summary>
		public ICommand EmbedMoney { get; }

		private bool CanExecuteEmbedMoney()
		{
			return this.IsConnected && !this.IsWriting;
		}

		private async Task ExecuteEmbedMoney()
		{
			// TODO
		}

		/// <summary>
		/// Command to embed a reference to a thing
		/// </summary>
		public ICommand EmbedThing { get; }

		private bool CanExecuteEmbedThing()
		{
			return this.IsConnected && !this.IsWriting;
		}

		private async Task ExecuteEmbedThing()
		{
			// TODO
		}

		/// <summary>
		/// Command executed when a multi-media-link with the xmpp URI scheme is clicked.
		/// </summary>
		public ICommand XmppUriClicked { get; }

		private async Task ExecuteXmppUriClicked(object Parameter)
		{
		}
	}
}