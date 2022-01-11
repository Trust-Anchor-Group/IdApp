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
using EDaler;
using EDaler.Uris;
using Waher.Content;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Persistence;
using Waher.Persistence.Filters;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts.MyContracts;
using IdApp.Pages.Contracts.MyContracts.ObjectModel;
using IdApp.Pages.Things.MyThings;
using IdApp.Pages.Wallet;
using IdApp.Pages.Wallet.SendPayment;
using IdApp.Popups.Xmpp.SubscribeTo;
using IdApp.Services;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Messages;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using IdApp.Services.UI.QR;
using IdApp.Services.Contracts;
using IdApp.Services.ThingRegistries;
using IdApp.Services.Wallet;

namespace IdApp.Pages.Contacts.Chat
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class ChatViewModel : NeuronViewModel, IChatView
	{
		private const int MessageBatchSize = 50;

		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private TaskCompletionSource<bool> waitUntilBound = new TaskCompletionSource<bool>();

		/// <summary>
		/// Creates an instance of the <see cref="ChatViewModel"/> class.
		/// </summary>
		public ChatViewModel()
			: this(null, null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ChatViewModel"/> class.
		/// For unit tests.
		/// </summary>
		/// <param name="NeuronService">The Neuron service for XMPP communication.</param>
		/// <param name="UiSerializer">The dispatcher to use for alerts and accessing the main thread.</param>
		/// <param name="TagProfile">TAG Profie service.</param>
		/// <param name="NavigationService">Navigation service.</param>
		/// <param name="LogService">Log service.</param>
		protected internal ChatViewModel(INeuronService NeuronService, IUiSerializer UiSerializer,
			ITagProfile TagProfile, INavigationService NavigationService, ILogService LogService)
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

			this.MessageSelected = new Command(async Parameter => await this.ExecuteMessageSelected(Parameter));
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out ChatNavigationArgs args))
			{
				this.LegalId = args.LegalId;
				this.BareJid = args.BareJid;
				this.FriendlyName = args.FriendlyName;
			}
			else
			{
				this.LegalId = string.Empty;
				this.BareJid = string.Empty;
				this.FriendlyName = string.Empty;
			}

			IEnumerable<ChatMessage> Messages = await Database.Find<ChatMessage>(0, MessageBatchSize, new FilterFieldEqualTo("RemoteBareJid", this.BareJid), "-Created");

			int c = MessageBatchSize;
			this.Messages.Clear();
			foreach (ChatMessage Message in Messages)
			{
				await Message.GenerateXaml(this);
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
			BindableProperty.Create("BareJid", typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Bare JID of remote party
		/// </summary>
		public string BareJid
		{
			get { return (string)GetValue(BareJidProperty); }
			set { SetValue(BareJidProperty, value); }
		}

		/// <summary>
		/// <see cref="LegalId"/>
		/// </summary>
		public static readonly BindableProperty LegalIdProperty =
			BindableProperty.Create("LegalId", typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Bare JID of remote party
		/// </summary>
		public string LegalId
		{
			get { return (string)GetValue(LegalIdProperty); }
			set { SetValue(LegalIdProperty, value); }
		}

		/// <summary>
		/// <see cref="FriendlyName"/>
		/// </summary>
		public static readonly BindableProperty FriendlyNameProperty =
			BindableProperty.Create("FriendlyName", typeof(string), typeof(ChatViewModel), default(string));

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
			BindableProperty.Create("MarkdownInput", typeof(string), typeof(ChatViewModel), default(string));

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
		/// <see cref="MessageId"/>
		/// </summary>
		public static readonly BindableProperty MessageIdProperty =
			BindableProperty.Create("MessageId", typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		public string MessageId
		{
			get { return (string)GetValue(MessageIdProperty); }
			set
			{
				SetValue(MessageIdProperty, value);
				this.IsWriting = !string.IsNullOrEmpty(value);
				this.EvaluateAllCommands();
			}
		}

		/// <summary>
		/// <see cref="ExistsMoreMessages"/>
		/// </summary>
		public static readonly BindableProperty ExistsMoreMessagesProperty =
			BindableProperty.Create("ExistsMoreMessages", typeof(bool), typeof(ChatViewModel), default(bool));

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
			BindableProperty.Create("IsWriting", typeof(bool), typeof(ChatViewModel), default(bool));

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
		/// External message has been received
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task MessageAdded(ChatMessage Message)
		{
			await Message.GenerateXaml(this);

			this.UiSerializer.BeginInvokeOnMainThread(() => this.Messages.Insert(0, Message));
		}

		/// <summary>
		/// External message has been updated
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task MessageUpdated(ChatMessage Message)
		{
			int i = 0;

			await Message.GenerateXaml(this);

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
			await this.ExecuteSendMessage(this.MessageId, this.MarkdownInput);
			this.MarkdownInput = string.Empty;
			this.MessageId = string.Empty;
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
					MessageType = Services.Messages.MessageType.Sent,
					Html = HtmlDocument.GetBody(await Doc.GenerateHTML()),
					PlainText = (await Doc.GeneratePlainText()).Trim(),
					Markdown = MarkdownInput
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
					await this.MessageAdded(Message);
				}
				else
				{
					ChatMessage Old = await Database.TryLoadObject<ChatMessage>(ReplaceObjectId);

					if (Old is null)
					{
						ReplaceObjectId = null;
						await Database.Insert(Message);

						await this.MessageAdded(Message);
					}
					else
					{
						Old.Updated = Message.Created;
						Old.Html = Message.Html;
						Old.PlainText = Message.PlainText;
						Old.Markdown = Message.Markdown;

						await Database.Update(Old);

						Message = Old;

						await this.MessageUpdated(Message);
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
				await Message.GenerateXaml(this);
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

			await this.waitUntilBound.Task;     // Wait until view is bound again.

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
			TaskCompletionSource<Contract> SelectedContract = new TaskCompletionSource<Contract>();

			await this.navigationService.GoToAsync(nameof(MyContractsPage), new MyContractsNavigationArgs(
				ContractsListMode.MyContracts, SelectedContract));

			Contract Contract = await SelectedContract.Task;
			if (Contract is null)
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			if (Contract.Visibility >= ContractVisibility.Public)
			{
				string FriendlyName = await ContractModel.GetName(Contract, this.TagProfile, this.NeuronService);
				await this.ExecuteSendMessage(string.Empty, "![" + MarkdownDocument.Encode(FriendlyName) + "](" + Contract.ContractIdUriString + ")");
			}
			else
			{
				StringBuilder Markdown = new StringBuilder();

				Markdown.AppendLine("```iotsc");

				Contract.Serialize(Markdown, true, true, true, true, true, true, true);

				Markdown.AppendLine();
				Markdown.AppendLine("```");

				await this.ExecuteSendMessage(string.Empty, Markdown.ToString());
			}
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
			StringBuilder sb = new StringBuilder();

			sb.Append("edaler:");

			if (!string.IsNullOrEmpty(this.LegalId))
			{
				sb.Append("ti=");
				sb.Append(this.LegalId);
			}
			else if (!string.IsNullOrEmpty(this.BareJid))
			{
				sb.Append("t=");
				sb.Append(this.BareJid);
			}
			else
				return;

			Balance CurrentBalance = await this.NeuronService.Wallet.GetBalanceAsync();

			sb.Append(";cu=");
			sb.Append(CurrentBalance.Currency);

			if (!EDalerUri.TryParse(sb.ToString(), out EDalerUri Parsed))
				return;

			TaskCompletionSource<string> UriToSend = new TaskCompletionSource<string>();

			await this.navigationService.GoToAsync(nameof(SendPaymentPage), new EDalerUriNavigationArgs(Parsed, 
				this.FriendlyName, UriToSend));

			string Uri = await UriToSend.Task;
			if (string.IsNullOrEmpty(Uri) || !EDalerUri.TryParse(Uri, out Parsed))
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			sb.Clear();

			sb.Append(Parsed.Amount.ToString());

			if (Parsed.AmountExtra.HasValue)
			{
				sb.Append(" (+");
				sb.Append(Parsed.AmountExtra.Value.ToString());
				sb.Append(")");
			}

			sb.Append(" ");
			sb.Append(Parsed.Currency);

			await this.ExecuteSendMessage(string.Empty, "![" + sb.ToString() + "](" + Uri + ")");
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
			TaskCompletionSource<ContactInfo> ThingToShare = new TaskCompletionSource<ContactInfo>();

			await this.navigationService.GoToAsync(nameof(MyThingsPage), new MyThingsNavigationArgs(ThingToShare));

			ContactInfo Thing = await ThingToShare.Task;
			if (Thing is null)
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			StringBuilder sb = new StringBuilder();

			sb.Append("![");
			sb.Append(MarkdownDocument.Encode(Thing.FriendlyName));
			sb.Append("](iotdisco:JID=");
			sb.Append(Thing.BareJid);

			if (!string.IsNullOrEmpty(Thing.SourceId))
			{
				sb.Append(";SID=");
				sb.Append(Thing.SourceId);
			}

			if (!string.IsNullOrEmpty(Thing.Partition))
			{
				sb.Append(";PT=");
				sb.Append(Thing.Partition);
			}

			if (!string.IsNullOrEmpty(Thing.NodeId))
			{
				sb.Append(";NID=");
				sb.Append(Thing.NodeId);
			}

			sb.Append(")");

			await this.ExecuteSendMessage(string.Empty, sb.ToString());
		}

		/// <summary>
		/// Command executed when a message has been selected (or deselected) in the list view.
		/// </summary>
		public ICommand MessageSelected { get; }

		private Task ExecuteMessageSelected(object Parameter)
		{
			if (!(Parameter is ChatMessage Message) || Message.MessageType != Services.Messages.MessageType.Sent)
			{
				this.MarkdownInput = string.Empty;
				this.MessageId = string.Empty;
			}
			else
			{
				this.MarkdownInput = Message.Markdown;
				this.MessageId = Message.ObjectId;
			}

			this.EvaluateAllCommands();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Called when a Multi-media URI link using the XMPP URI scheme.
		/// </summary>
		/// <param name="Message">Message containing the URI.</param>
		/// <param name="Uri">URI</param>
		/// <param name="Scheme">URI Scheme</param>
		public Task ExecuteUriClicked(ChatMessage Message, string Uri, UriScheme Scheme)
		{
			switch (Scheme)
			{
				case UriScheme.Xmpp:
					return ProcessXmppUri(Uri, this.NeuronService, this.TagProfile);

				default:
					return QrCode.OpenUrl(Uri, this.logService, this.NeuronService, this.UiSerializer,
						App.Instantiate<IContractOrchestratorService>(),
						App.Instantiate<IThingRegistryOrchestratorService>(),
						App.Instantiate<IEDalerOrchestratorService>());
			}
		}

		/// <summary>
		/// Processes an XMPP URI
		/// </summary>
		/// <param name="Uri">XMPP URI</param>
		/// <returns>If URI could be processed.</returns>
		public static Task<bool> ProcessXmppUri(string Uri)
		{
			return ProcessXmppUri(Uri, App.Instantiate<INeuronService>(), App.Instantiate<ITagProfile>());
		}

		/// <summary>
		/// Processes an XMPP URI
		/// </summary>
		/// <param name="Uri">XMPP URI</param>
		/// <param name="NeuronService">Neuron Service</param>
		/// <param name="TagProfile">TAG Profile</param>
		/// <returns>If URI could be processed.</returns>
		public static async Task<bool> ProcessXmppUri(string Uri, INeuronService NeuronService, ITagProfile TagProfile)
		{
			int i = Uri.IndexOf(':');
			if (i < 0)
				return false;

			string Jid = Uri.Substring(i + 1).TrimStart();
			string Command;

			i = Jid.IndexOf('?');
			if (i < 0)
				Command = "subscribe";
			else
			{
				Command = Jid.Substring(i + 1).TrimStart();
				Jid = Jid.Substring(0, i).TrimEnd();
			}

			Jid = System.Web.HttpUtility.UrlDecode(Jid);
			Jid = XmppClient.GetBareJID(Jid);

			switch (Command.ToLower())
			{
				case "subscribe":
					SubscribeToPopupPage SubscribeToPopupPage = new SubscribeToPopupPage(Jid);

					await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(SubscribeToPopupPage);
					bool? SubscribeTo = await SubscribeToPopupPage.Result;

					if (SubscribeTo.HasValue && SubscribeTo.Value)
					{
						string IdXml;

						if (TagProfile.LegalIdentity is null)
							IdXml = string.Empty;
						else
						{
							StringBuilder Xml = new StringBuilder();
							TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
							IdXml = Xml.ToString();
						}

						NeuronService.Xmpp.RequestPresenceSubscription(Jid, IdXml);
					}
					return true;

				case "unsubscribe":
					// TODO
					return false;

				case "remove":
					RosterItem Item = NeuronService.Xmpp.GetRosterItem(Jid);
					// TODO
					return false;

				default:
					return false;
			}
		}
	}
}