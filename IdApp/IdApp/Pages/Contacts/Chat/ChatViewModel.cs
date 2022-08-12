using EDaler;
using EDaler.Uris;
using IdApp.Converters;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts.MyContracts;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Pages.Things.MyThings;
using IdApp.Pages.Wallet;
using IdApp.Pages.Wallet.MyTokens;
using IdApp.Pages.Wallet.MyWallet.ObjectModels;
using IdApp.Pages.Wallet.SendPayment;
using IdApp.Pages.Wallet.TokenDetails;
using IdApp.Popups.Xmpp.SubscribeTo;
using IdApp.Resx;
using IdApp.Services;
using IdApp.Services.Messages;
using IdApp.Services.Notification;
using IdApp.Services.Tag;
using IdApp.Services.UI.QR;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using Plugin.Media;
using Plugin.Media.Abstractions;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using Waher.Content;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Contacts.Chat
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class ChatViewModel : XmppViewModel, IChatView, ILinkableView
	{
		private TaskCompletionSource<bool> waitUntilBound = new();

		/// <summary>
		/// Creates an instance of the <see cref="ChatViewModel"/> class.
		/// </summary>
		protected internal ChatViewModel()
			: base()
		{
			this.ExpandButtons = new Command(_ => this.IsButtonExpanded = !this.IsButtonExpanded);
			this.SendCommand = new Command(async _ => await this.ExecuteSendMessage(), _ => this.CanExecuteSendMessage());
			this.CancelCommand = new Command(async _ => await this.ExecuteCancelMessage(), _ => this.CanExecuteCancelMessage());
			this.LoadMoreMessages = new Command(async _ => await this.ExecuteLoadMessagesAsync(), _ => this.CanExecuteLoadMoreMessages());
			this.TakePhoto = new Command(async _ => await this.ExecuteTakePhoto(), _ => this.CanExecuteTakePhoto());
			this.EmbedFile = new Command(async _ => await this.ExecuteEmbedFile(), _ => this.CanExecuteEmbedFile());
			this.EmbedId = new Command(async _ => await this.ExecuteEmbedId(), _ => this.CanExecuteEmbedId());
			this.EmbedContract = new Command(async _ => await this.ExecuteEmbedContract(), _ => this.CanExecuteEmbedContract());
			this.EmbedMoney = new Command(async _ => await this.ExecuteEmbedMoney(), _ => this.CanExecuteEmbedMoney());
			this.EmbedToken = new Command(async _ => await this.ExecuteEmbedToken(), _ => this.CanExecuteEmbedToken());
			this.EmbedThing = new Command(async _ => await this.ExecuteEmbedThing(), _ => this.CanExecuteEmbedThing());

			this.MessageSelected = new Command(async Parameter => await this.ExecuteMessageSelected(Parameter));
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out ChatNavigationArgs args, this.UniqueId))
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

			await this.ExecuteLoadMessagesAsync(false);

			this.EvaluateAllCommands();
			this.waitUntilBound.TrySetResult(true);

			await this.NotificationService.DeleteEvents(EventButton.Contacts, this.BareJid);
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			await base.DoUnbind();

			this.waitUntilBound = new TaskCompletionSource<bool>();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.SendCommand, this.CancelCommand, this.LoadMoreMessages, this.TakePhoto, this.EmbedFile,
				this.EmbedId, this.EmbedContract, this.EmbedMoney, this.EmbedToken, this.EmbedThing);
		}

		/// <inheritdoc/>
		protected override void XmppService_ConnectionStateChanged(object Sender, ConnectionStateChangedEventArgs e)
		{
			base.XmppService_ConnectionStateChanged(Sender, e);
			this.UiSerializer.BeginInvokeOnMainThread(() => this.EvaluateAllCommands());
		}

		/// <summary>
		/// <see cref="UniqueId"/>
		/// </summary>
		public static readonly BindableProperty UniqueIdProperty =
			BindableProperty.Create(nameof(UniqueId), typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Set the views unique ID
		/// </summary>
		public string UniqueId
		{
			get => (string)this.GetValue(UniqueIdProperty);
			set => this.SetValue(UniqueIdProperty, value);
		}

		/// <summary>
		/// <see cref="BareJid"/>
		/// </summary>
		public static readonly BindableProperty BareJidProperty =
			BindableProperty.Create(nameof(BareJid), typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Bare JID of remote party
		/// </summary>
		public string BareJid
		{
			get => (string)this.GetValue(BareJidProperty);
			set => this.SetValue(BareJidProperty, value);
		}

		/// <summary>
		/// <see cref="LegalId"/>
		/// </summary>
		public static readonly BindableProperty LegalIdProperty =
			BindableProperty.Create(nameof(LegalId), typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Bare JID of remote party
		/// </summary>
		public string LegalId
		{
			get => (string)this.GetValue(LegalIdProperty);
			set => this.SetValue(LegalIdProperty, value);
		}

		/// <summary>
		/// <see cref="FriendlyName"/>
		/// </summary>
		public static readonly BindableProperty FriendlyNameProperty =
			BindableProperty.Create(nameof(FriendlyName), typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Friendly name of remote party
		/// </summary>
		public string FriendlyName
		{
			get => (string)this.GetValue(FriendlyNameProperty);
			set => this.SetValue(FriendlyNameProperty, value);
		}

		/// <summary>
		/// <see cref="MarkdownInput"/>
		/// </summary>
		public static readonly BindableProperty MarkdownInputProperty =
			BindableProperty.Create(nameof(MarkdownInput), typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		public string MarkdownInput
		{
			get => (string)this.GetValue(MarkdownInputProperty);
			set
			{
				this.SetValue(MarkdownInputProperty, value);
				this.IsWriting = !string.IsNullOrEmpty(value);
				this.EvaluateAllCommands();

				if (!string.IsNullOrEmpty(value))
				{
					MessagingCenter.Send<object>(this, Constants.MessagingCenter.ChatEditorFocus);
				}
			}
		}

		/// <summary>
		/// <see cref="MessageId"/>
		/// </summary>
		public static readonly BindableProperty MessageIdProperty =
			BindableProperty.Create(nameof(MessageId), typeof(string), typeof(ChatViewModel), default(string));

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		public string MessageId
		{
			get => (string)this.GetValue(MessageIdProperty);
			set
			{
				this.SetValue(MessageIdProperty, value);
				this.IsWriting = !string.IsNullOrEmpty(value);
				this.EvaluateAllCommands();
			}
		}

		/// <summary>
		/// <see cref="ExistsMoreMessages"/>
		/// </summary>
		public static readonly BindableProperty ExistsMoreMessagesProperty =
			BindableProperty.Create(nameof(ExistsMoreMessages), typeof(bool), typeof(ChatViewModel), default(bool));

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		public bool ExistsMoreMessages
		{
			get => (bool)this.GetValue(ExistsMoreMessagesProperty);
			set
			{
				this.SetValue(ExistsMoreMessagesProperty, value);
				this.EvaluateCommands(this.LoadMoreMessages);
			}
		}

		/// <summary>
		/// <see cref="IsWriting"/>
		/// </summary>
		public static readonly BindableProperty IsWritingProperty =
			BindableProperty.Create(nameof(IsWriting), typeof(bool), typeof(ChatViewModel), default(bool));

		/// <summary>
		/// If the user is writing markdown.
		/// </summary>
		public bool IsWriting
		{
			get => (bool)this.GetValue(IsWritingProperty);
			set
			{
				this.IsButtonExpanded = false;
				this.SetValue(IsWritingProperty, value);
			}
		}

		/// <summary>
		/// Holds the list of chat messages to display.
		/// </summary>
		public ObservableRangeCollection<ChatMessage> Messages { get; } = new();

		/// <summary>
		/// External message has been received
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task MessageAddedAsync(ChatMessage Message)
		{
			try
			{
				await Message.GenerateXaml(this);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				return;
			}

			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				try
				{
					int i = 0;

					for (; i < this.Messages.Count; i++)
					{
						ChatMessage Item = this.Messages[i];

						if (Item.Created <= Message.Created)
							break;
					}

					if (i >= this.Messages.Count)
						this.Messages.Add(Message);
					else if (this.Messages[i].ObjectId != Message.ObjectId)
						this.Messages.Insert(i, Message);

					this.EnsureFirstMessageIsEmpty();
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			});
		}

		/// <summary>
		/// External message has been updated
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task MessageUpdatedAsync(ChatMessage Message)
		{
			try
			{
				await Message.GenerateXaml(this);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				return;
			}

			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				try
				{
					for (int i = 0; i < this.Messages.Count; i++)
					{
						ChatMessage Item = this.Messages[i];

						if (Item.ObjectId == Message.ObjectId)
						{
							this.Messages[i] = Message;
							break;
						}

						this.EnsureFirstMessageIsEmpty();
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			});
		}

		private async Task ExecuteLoadMessagesAsync(bool LoadMore = true)
		{
			IEnumerable<ChatMessage> Messages = null;
			int c = Constants.BatchSizes.MessageBatchSize;

			try
			{
				this.ExistsMoreMessages = false;

				DateTime LastTime = LoadMore ? this.Messages[^1].Created : DateTime.MaxValue;

				Messages = await Database.Find<ChatMessage>(0, Constants.BatchSizes.MessageBatchSize,
					new FilterAnd(
						new FilterFieldEqualTo("RemoteBareJid", this.BareJid),
						new FilterFieldLesserThan("Created", LastTime)), "-Created");

				foreach (ChatMessage Message in Messages)
				{
					await Message.GenerateXaml(this);
					c--;
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				this.ExistsMoreMessages = false;
				return;
			}

			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				try
				{
					this.MergeObservableCollections(LoadMore, Messages.ToList());
					this.ExistsMoreMessages = c <= 0;
					this.EnsureFirstMessageIsEmpty();
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					this.ExistsMoreMessages = false;
				}
			});
		}

		private void MergeObservableCollections(bool LoadMore, List<ChatMessage> NewMessages)
		{
			if (LoadMore || (this.Messages.Count == 0))
			{
				this.Messages.AddRange(NewMessages);
				return;
			}

			List<ChatMessage> RemoveItems = this.Messages.Where(oel => NewMessages.All(nel => nel.UniqueName != oel.UniqueName)).ToList();
			this.Messages.RemoveRange(RemoveItems);

			for (int i = 0; i < NewMessages.Count; i++)
			{
				ChatMessage Item = NewMessages[i];

				if (i >= this.Messages.Count)
					this.Messages.Add(Item);
				else if (this.Messages[i].UniqueName != Item.UniqueName)
					this.Messages.Insert(i, Item);
			}
		}

		private void EnsureFirstMessageIsEmpty()
		{
			if (this.Messages.Count > 0 && this.Messages[0].MessageType != Services.Messages.MessageType.Empty)
			{
				this.Messages.Insert(0, ChatMessage.Empty);
			}
		}

		/// <summary>
		/// <see cref="IsButtonExpanded"/>
		/// </summary>
		public static readonly BindableProperty IsButtonExpandedProperty =
			BindableProperty.Create(nameof(IsButtonExpanded), typeof(bool), typeof(ChatViewModel), default(bool));

		/// <summary>
		/// If the button is expanded
		/// </summary>
		public bool IsButtonExpanded
		{
			get => (bool)this.GetValue(IsButtonExpandedProperty);
			set => this.SetValue(IsButtonExpandedProperty, value);
		}

		/// <summary>
		/// Command to expand the buttons
		/// </summary>
		public ICommand ExpandButtons { get; }

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
			await this.ExecuteCancelMessage();
		}

		private Task ExecuteSendMessage(string ReplaceObjectId, string MarkdownInput)
		{
			return ExecuteSendMessage(ReplaceObjectId, MarkdownInput, this.BareJid, this);
		}

		/// <summary>
		/// Sends a Markdown-formatted chat message
		/// </summary>
		/// <param name="ReplaceObjectId">ID of message being updated, or the empty string.</param>
		/// <param name="MarkdownInput">Markdown input.</param>
		/// <param name="BareJid">Bare JID of recipient.</param>
		/// <param name="ServiceReferences">Service references.</param>
		public static async Task ExecuteSendMessage(string ReplaceObjectId, string MarkdownInput, string BareJid, ServiceReferences ServiceReferences)
		{
			try
			{
				if (string.IsNullOrEmpty(MarkdownInput))
					return;

				MarkdownSettings Settings = new()
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

				ChatMessage Message = new()
				{
					Created = DateTime.UtcNow,
					RemoteBareJid = BareJid,
					RemoteObjectId = string.Empty,
					MessageType = Services.Messages.MessageType.Sent,
					Html = HtmlDocument.GetBody(await Doc.GenerateHTML()),
					PlainText = (await Doc.GeneratePlainText()).Trim(),
					Markdown = MarkdownInput
				};

				StringBuilder Xml = new();

				Xml.Append("<content xmlns=\"urn:xmpp:content\" type=\"text/markdown\">");
				Xml.Append(XML.Encode(MarkdownInput));
				Xml.Append("</content><html xmlns='http://jabber.org/protocol/xhtml-im'><body xmlns='http://www.w3.org/1999/xhtml'>");

				HtmlDocument HtmlDoc = new("<root>" + Message.Html + "</root>");

				foreach (HtmlNode N in (HtmlDoc.Body ?? HtmlDoc.Root).Children)
					N.Export(Xml);

				Xml.Append("</body></html>");

				if (!string.IsNullOrEmpty(ReplaceObjectId))
				{
					Xml.Append("<replace id='");
					Xml.Append(ReplaceObjectId);
					Xml.Append("' xmlns='urn:xmpp:message-correct:0'/>");
				}

				if (string.IsNullOrEmpty(ReplaceObjectId))
				{
					await Database.Insert(Message);

					if (ServiceReferences is ChatViewModel ChatViewModel)
						await ChatViewModel.MessageAddedAsync(Message);
				}
				else
				{
					ChatMessage Old = await Database.TryLoadObject<ChatMessage>(ReplaceObjectId);

					if (Old is null)
					{
						ReplaceObjectId = null;
						await Database.Insert(Message);

						if (ServiceReferences is ChatViewModel ChatViewModel)
							await ChatViewModel.MessageAddedAsync(Message);
					}
					else
					{
						Old.Updated = Message.Created;
						Old.Html = Message.Html;
						Old.PlainText = Message.PlainText;
						Old.Markdown = Message.Markdown;

						await Database.Update(Old);

						Message = Old;

						if (ServiceReferences is ChatViewModel ChatViewModel)
							await ChatViewModel.MessageUpdatedAsync(Message);
					}
				}

				ServiceReferences.XmppService.Xmpp.SendMessage(QoSLevel.Unacknowledged, Waher.Networking.XMPP.MessageType.Chat, Message.ObjectId,
					BareJid, Xml.ToString(), Message.PlainText, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
				// TODO: End-to-End encryption
			}
			catch (Exception ex)
			{
				await ServiceReferences.UiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// The command to bind to for sending user input
		/// </summary>
		public ICommand CancelCommand { get; }

		private bool CanExecuteCancelMessage()
		{
			return this.IsConnected && !string.IsNullOrEmpty(this.MarkdownInput);
		}

		private Task ExecuteCancelMessage()
		{
			this.MarkdownInput = string.Empty;
			this.MessageId = string.Empty;

			return Task.CompletedTask;
		}

		/// <summary>
		/// The command to bind to for loading more messages.
		/// </summary>
		public ICommand LoadMoreMessages { get; }

		private bool CanExecuteLoadMoreMessages()
		{
			return this.ExistsMoreMessages && this.Messages.Count > 0;
		}

		/// <summary>
		/// Command to take and send a photo
		/// </summary>
		public ICommand TakePhoto { get; }

		private bool CanExecuteTakePhoto()
		{
			return this.IsConnected && !this.IsWriting && this.XmppService.Contracts.FileUploadIsSupported;
		}

		private async Task ExecuteTakePhoto()
		{
			if (!this.XmppService.Contracts.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.ServerDoesNotSupportFileUpload);
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
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported + ": " + ex.Message);
					return;
				}

				if (capturedPhoto is not null)
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
				catch (Exception ex)
				{
					await this.UiSerializer.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported + ": " + ex.Message);
					return;
				}

				if (capturedPhoto is not null)
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
				if (!await this.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
				{
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain));
					return;
				}

				string FileName = Path.GetFileName(FilePath);
				HttpFileUploadEventArgs Slot = await this.XmppService.Contracts.FileUploadClient.RequestUploadSlotAsync(
					FileName, ContentType, Bin.Length);

				if (!Slot.Ok)
					throw Slot.StanzaError ?? new Exception(Slot.ErrorText);

				await Slot.PUT(Bin, ContentType, (int)Constants.Timeouts.UploadFile.TotalMilliseconds);

				StringBuilder MarkdownBuilder = new($"![{MarkdownDocument.Encode(FileName)}]({Slot.GetUrl}");

				SKImageInfo ImageInfo = SKBitmap.DecodeBounds(Bin);
				if (!ImageInfo.IsEmpty)
				{
					MarkdownBuilder.Append($" {ImageInfo.Width} {ImageInfo.Height}");
				}

				MarkdownBuilder.Append(")");

				await this.ExecuteSendMessage(string.Empty, MarkdownBuilder.ToString());

				// TODO: File Transfer instead of HTTP File Upload

				if (DeleteFile)
					File.Delete(FilePath);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
				this.LogService.LogException(ex);
				return;
			}
		}

		/// <summary>
		/// Command to embed a file
		/// </summary>
		public ICommand EmbedFile { get; }

		private bool CanExecuteEmbedFile()
		{
			return this.IsConnected && !this.IsWriting && this.XmppService.Contracts.FileUploadIsSupported;
		}

		private async Task ExecuteEmbedFile()
		{
			if (!this.XmppService.Contracts.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(AppResources.PickPhoto, AppResources.SelectingAPhotoIsNotSupported);
				return;
			}

			FileResult pickedPhoto = await MediaPicker.PickPhotoAsync();

			if (pickedPhoto is not null)
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
			TaskCompletionSource<ContactInfoModel> SelectedContact = new();

			await this.NavigationService.GoToAsync(nameof(MyContactsPage),
				new ContactListNavigationArgs(AppResources.SelectContactToPay, SelectedContact)
				{
					CanScanQrCode = true
				});

			ContactInfoModel Contact = await SelectedContact.Task;
			if (Contact is null)
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			if (Contact.LegalIdentity is not null)
			{
				StringBuilder Markdown = new();

				Markdown.Append("```");
				Markdown.AppendLine(Constants.UriSchemes.UriSchemeIotId);

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
			TaskCompletionSource<Contract> SelectedContract = new();

			await this.NavigationService.GoToAsync(nameof(MyContractsPage), new MyContractsNavigationArgs(
				ContractsListMode.MyContracts, SelectedContract));

			Contract Contract = await SelectedContract.Task;
			if (Contract is null)
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			StringBuilder Markdown = new();

			Markdown.Append("```");
			Markdown.AppendLine(Constants.UriSchemes.UriSchemeIotSc);

			Contract.Serialize(Markdown, true, true, true, true, true, true, true);

			Markdown.AppendLine();
			Markdown.AppendLine("```");

			await this.ExecuteSendMessage(string.Empty, Markdown.ToString());
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
			StringBuilder sb = new();

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

			Balance CurrentBalance = await this.XmppService.Wallet.GetBalanceAsync();

			sb.Append(";cu=");
			sb.Append(CurrentBalance.Currency);

			if (!EDalerUri.TryParse(sb.ToString(), out EDalerUri Parsed))
				return;

			TaskCompletionSource<string> UriToSend = new();

			await this.NavigationService.GoToAsync(nameof(SendPaymentPage), new EDalerUriNavigationArgs(Parsed,
				this.FriendlyName, UriToSend));

			string Uri = await UriToSend.Task;
			if (string.IsNullOrEmpty(Uri) || !EDalerUri.TryParse(Uri, out Parsed))
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			sb.Clear();
			sb.Append(MoneyToString.ToString(Parsed.Amount));

			if (Parsed.AmountExtra.HasValue)
			{
				sb.Append(" (+");
				sb.Append(MoneyToString.ToString(Parsed.AmountExtra.Value));
				sb.Append(")");
			}

			sb.Append(" ");
			sb.Append(Parsed.Currency);

			await this.ExecuteSendMessage(string.Empty, "![" + sb.ToString() + "](" + Uri + ")");
		}

		/// <summary>
		/// Command to embed a token reference
		/// </summary>
		public ICommand EmbedToken { get; }

		private bool CanExecuteEmbedToken()
		{
			return this.IsConnected && !this.IsWriting;
		}

		private async Task ExecuteEmbedToken()
		{
			MyTokensNavigationArgs Args = new();
			await this.NavigationService.GoToAsync(nameof(MyTokensPage), Args);

			TokenItem Selected = await Args.WaitForTokenSelection();
			if (Selected is null)
				return;

			await this.NavigationService.GoBackAsync();

			StringBuilder Markdown = new();

			Markdown.AppendLine("```nfeat");

			Selected.Token.Serialize(Markdown);

			Markdown.AppendLine();
			Markdown.AppendLine("```");

			await this.ExecuteSendMessage(string.Empty, Markdown.ToString());
			return;

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
			TaskCompletionSource<ContactInfo> ThingToShare = new();

			await this.NavigationService.GoToAsync(nameof(MyThingsPage), new MyThingsNavigationArgs(ThingToShare));

			ContactInfo Thing = await ThingToShare.Task;
			if (Thing is null)
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			StringBuilder sb = new();

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
			if (Parameter is ChatMessage Message)
			{
				switch (Message.MessageType)
				{
					case Services.Messages.MessageType.Sent:
						this.MessageId = Message.ObjectId;
						this.MarkdownInput = Message.Markdown;
						break;

					case Services.Messages.MessageType.Received:
						string s = Message.Markdown;
						if (string.IsNullOrEmpty(s))
							s = MarkdownDocument.Encode(Message.PlainText);

						string[] Rows = s.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

						StringBuilder Quote = new();

						foreach (string Row in Rows)
						{
							Quote.Append("> ");
							Quote.AppendLine(Row);
						}

						Quote.AppendLine();

						this.MessageId = string.Empty;
						this.MarkdownInput = Quote.ToString();
						break;
				}
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
		public async Task ExecuteUriClicked(ChatMessage Message, string Uri, UriScheme Scheme)
		{
			try
			{
				if (Scheme == UriScheme.Xmpp)
					await ProcessXmppUri(Uri, this.XmppService, this.TagProfile);
				else
				{
					int i = Uri.IndexOf(':');
					if (i < 0)
						return;

					string s = Uri[(i + 1)..].Trim();
					if (s.StartsWith("<") && s.EndsWith(">"))  // XML
					{
						XmlDocument Doc = new();
						Doc.LoadXml(s);

						switch (Scheme)
						{
							case UriScheme.IotId:
								LegalIdentity Id = LegalIdentity.Parse(Doc.DocumentElement);
								await this.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Id, null));
								break;

							case UriScheme.IotSc:
								ParsedContract ParsedContract = await Contract.Parse(Doc.DocumentElement);
								await this.NavigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(ParsedContract.Contract, false));
								break;

							case UriScheme.NeuroFeature:
								if (!Token.TryParse(Doc.DocumentElement, out Token ParsedToken))
									throw new Exception(AppResources.InvalidNeuroFeatureToken);

								if (!this.NotificationService.TryGetNotificationEvents(EventButton.Wallet, ParsedToken.TokenId, out NotificationEvent[] Events))
									Events = new NotificationEvent[0];

								await this.NavigationService.GoToAsync(nameof(TokenDetailsPage),
									new TokenDetailsNavigationArgs(new TokenItem(ParsedToken, this, Events)) { ReturnCounter = 1 });
								break;

							default:
								return;
						}
					}
					else
						await QrCode.OpenUrl(Uri);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		/// <summary>
		/// Processes an XMPP URI
		/// </summary>
		/// <param name="Uri">XMPP URI</param>
		/// <returns>If URI could be processed.</returns>
		public static Task<bool> ProcessXmppUri(string Uri)
		{
			return ProcessXmppUri(Uri, App.Instantiate<IXmppService>(), App.Instantiate<ITagProfile>());
		}

		/// <summary>
		/// Processes an XMPP URI
		/// </summary>
		/// <param name="Uri">XMPP URI</param>
		/// <param name="XmppService">XMPP Service</param>
		/// <param name="TagProfile">TAG Profile</param>
		/// <returns>If URI could be processed.</returns>
		public static async Task<bool> ProcessXmppUri(string Uri, IXmppService XmppService, ITagProfile TagProfile)
		{
			int i = Uri.IndexOf(':');
			if (i < 0)
				return false;

			string Jid = Uri[(i + 1)..].TrimStart();
			string Command;

			i = Jid.IndexOf('?');
			if (i < 0)
				Command = "subscribe";
			else
			{
				Command = Jid[(i + 1)..].TrimStart();
				Jid = Jid[..i].TrimEnd();
			}

			Jid = System.Web.HttpUtility.UrlDecode(Jid);
			Jid = XmppClient.GetBareJID(Jid);

			switch (Command.ToLower())
			{
				case "subscribe":
					SubscribeToPopupPage SubscribeToPopupPage = new(Jid);

					await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(SubscribeToPopupPage);
					bool? SubscribeTo = await SubscribeToPopupPage.Result;

					if (SubscribeTo.HasValue && SubscribeTo.Value)
					{
						string IdXml;

						if (TagProfile.LegalIdentity is null)
							IdXml = string.Empty;
						else
						{
							StringBuilder Xml = new();
							TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
							IdXml = Xml.ToString();
						}

						XmppService.Xmpp.RequestPresenceSubscription(Jid, IdXml);
					}
					return true;

				case "unsubscribe":
					// TODO
					return false;

				case "remove":
					XmppService.Xmpp.GetRosterItem(Jid);
					// TODO
					return false;

				default:
					return false;
			}
		}

		#region ILinkableView

		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		public bool IsLinkable => true;

		/// <summary>
		/// Link to the current view
		/// </summary>
		public string Link => Constants.UriSchemes.UriSchemeXmpp + ":" + this.BareJid;

		/// <summary>
		/// Title of the current view
		/// </summary>
		public Task<string> Title => Task.FromResult<string>(this.FriendlyName);

		/// <summary>
		/// If linkable view has media associated with link.
		/// </summary>
		public bool HasMedia => false;

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		public byte[] Media => null;

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		public string MediaContentType => null;

		#endregion

	}
}
