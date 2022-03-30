using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using EDaler;
using IdApp.Extensions;
using IdApp.Pages.Registration.Registration;
using IdApp.Services;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Xamarin.Essentials;
using Xamarin.Forms;
using IdApp.Pages.Identity.TransferIdentity;
using IdApp.Popups.Pin.ChangePin;
using IdApp.Services.Contracts;
using IdApp.Services.Xmpp;
using IdApp.Services.UI;
using IdApp.Services.UI.Photos;
using IdApp.Services.Data.Countries;
using IdApp.Pages.Contacts.Chat;
using IdApp.Popups.Xmpp.RemoveSubscription;
using IdApp.Resx;

namespace IdApp.Pages.Identity.ViewIdentity
{
	/// <summary>
	/// The view model to bind to for when displaying identities.
	/// </summary>
	public class ViewIdentityViewModel : XmppViewModel
	{
		private SignaturePetitionEventArgs identityToReview;
		private readonly PhotosLoader photosLoader;

		/// <summary>
		/// Creates an instance of the <see cref="ViewIdentityViewModel"/> class.
		/// </summary>
		public ViewIdentityViewModel()
			: base()
		{
			this.Photos = new ObservableCollection<Photo>();
			this.photosLoader = new PhotosLoader(this.Photos);

			this.ApproveCommand = new Command(async _ => await Approve(), _ => this.IsConnected);
			this.RejectCommand = new Command(async _ => await Reject(), _ => this.IsConnected);
			this.RevokeCommand = new Command(async _ => await Revoke(), _ => this.IsConnected);
			this.TransferCommand = new Command(async _ => await Transfer(), _ => this.IsConnected);
			this.CompromiseCommand = new Command(async _ => await Compromise(), _ => this.IsConnected);
			this.ChangePinCommand = new Command(async _ => await ChangePin(), _ => this.IsConnected);
			this.CopyCommand = new Command(_ => this.CopyHtmlToClipboard());
			this.AddContactCommand = new Command(async _ => await this.AddContact(), _ => this.ThirdPartyNotInContacts);
			this.RemoveContactCommand = new Command(async _ => await this.RemoveContact(), _ => this.ThirdPartyInContacts);
			this.SendPaymentToCommand = new Command(async _ => await this.SendPaymentTo(), _ => this.ThirdParty);
			this.ChatCommand = new Command(async _ => await this.OpenChat(), _ => this.ThirdParty);
			this.SubscribeToCommand = new Command(async _ => await this.SubscribeTo(), _ => this.NotSubscribed);
			this.UnsubscribeFromCommand = new Command(async _ => await this.UnsubscribeFrom(), _ => this.Subscribed);
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out ViewIdentityNavigationArgs args))
			{
				this.LegalIdentity = args.Identity ?? this.TagProfile.LegalIdentity;
				this.identityToReview = args.IdentityToReview;
			}

			if (this.LegalIdentity is null)
			{
				this.LegalIdentity = this.TagProfile.LegalIdentity;
				this.identityToReview = null;
				this.IsPersonal = true;
			}
			else
				this.IsPersonal = false;

			AssignProperties();

			if (this.ThirdParty)
			{
				ContactInfo Info = await ContactInfo.FindByBareJid(this.BareJid);

				if (!(Info is null) &&
					Info.LegalId != this.LegalId &&
					Info.LegalIdentity.Created < this.LegalIdentity.Created &&
					this.LegalIdentity.State == IdentityState.Approved)
				{
					Info.LegalId = this.LegalId;
					Info.LegalIdentity = this.LegalIdentity;
					Info.FriendlyName = this.GetFriendlyName();

					await Database.Update(Info);
					await Database.Provider.Flush();
				}

				this.ThirdPartyNotInContacts = Info is null;
				this.ThirdPartyInContacts = !this.ThirdPartyNotInContacts;
			}
			else
			{
				this.ThirdPartyNotInContacts = false;
				this.ThirdPartyInContacts = false;
			}

			EvaluateAllCommands();

			this.TagProfile.Changed += TagProfile_Changed;
			this.XmppService.Contracts.LegalIdentityChanged += SmartContracts_LegalIdentityChanged;
			this.XmppService.Xmpp.OnRosterItemAdded += CheckRosterItem;
			this.XmppService.Xmpp.OnRosterItemRemoved += CheckRosterItem;
			this.XmppService.Xmpp.OnRosterItemUpdated += CheckRosterItem;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.photosLoader.CancelLoadPhotos();

			this.TagProfile.Changed -= TagProfile_Changed;
			this.XmppService.Contracts.LegalIdentityChanged -= SmartContracts_LegalIdentityChanged;
			this.XmppService.Xmpp.OnRosterItemAdded -= CheckRosterItem;
			this.XmppService.Xmpp.OnRosterItemRemoved -= CheckRosterItem;
			this.XmppService.Xmpp.OnRosterItemUpdated -= CheckRosterItem;

			this.LegalIdentity = null;

			await base.DoUnbind();
		}

		#region Properties

		/// <summary>
		/// Holds a list of photos associated with this identity.
		/// </summary>
		public ObservableCollection<Photo> Photos { get; }

		/// <summary>
		/// The command to bind to for approving an identity
		/// </summary>
		public ICommand ApproveCommand { get; }

		/// <summary>
		/// The command to bind to for rejecting an identity
		/// </summary>
		public ICommand RejectCommand { get; }

		/// <summary>
		/// The command to bind to for changing PIN.
		/// </summary>
		public ICommand ChangePinCommand { get; }

		/// <summary>
		/// The command to bind to for flagging an identity as compromised.
		/// </summary>
		public ICommand CompromiseCommand { get; }

		/// <summary>
		/// The command to bind to for revoking an identity
		/// </summary>
		public ICommand RevokeCommand { get; }

		/// <summary>
		/// The command to bind to for transferring an identity
		/// </summary>
		public ICommand TransferCommand { get; }

		/// <summary>
		/// The command for copying data to clipboard.
		/// </summary>
		public ICommand CopyCommand { get; }

		/// <summary>
		/// The command for adding the identity to the list of contacts.
		/// </summary>
		public ICommand AddContactCommand { get; }

		/// <summary>
		/// The command for removing the identity from the list of contacts.
		/// </summary>
		public ICommand RemoveContactCommand { get; }

		/// <summary>
		/// The command for sending a payment to entity.
		/// </summary>
		public ICommand SendPaymentToCommand { get; }

		/// <summary>
		/// The command for opening the chat page.
		/// </summary>
		public ICommand ChatCommand { get; }

		/// <summary>
		/// The command for subscribing to the presence of a contact
		/// </summary>
		public ICommand SubscribeToCommand { get; }

		/// <summary>
		/// The command for unsubscribing from the presence of a contact
		/// </summary>
		public ICommand UnsubscribeFromCommand { get; }

		#endregion

		private void AssignProperties()
		{
			this.Created = this.LegalIdentity?.Created ?? DateTime.MinValue;
			this.Updated = this.LegalIdentity?.Updated.GetDateOrNullIfMinValue();
			this.LegalId = this.LegalIdentity?.Id;

			if (!(this.identityToReview?.RequestorIdentity is null))
				this.BareJid = this.identityToReview.RequestorIdentity.GetJid(Constants.NotAvailableValue);
			else if (!(this.LegalIdentity is null))
				this.BareJid = this.LegalIdentity.GetJid(Constants.NotAvailableValue);
			else
				this.BareJid = Constants.NotAvailableValue;

			if (!(this.LegalIdentity?.ClientPubKey is null))
				this.PublicKey = Convert.ToBase64String(this.LegalIdentity.ClientPubKey);
			else
				this.PublicKey = string.Empty;

			this.State = this.LegalIdentity?.State ?? IdentityState.Rejected;
			this.From = this.LegalIdentity?.From.GetDateOrNullIfMinValue();
			this.To = this.LegalIdentity?.To.GetDateOrNullIfMinValue();

			if (!(this.LegalIdentity is null))
			{
				this.FirstName = this.LegalIdentity[Constants.XmppProperties.FirstName];
				this.MiddleNames = this.LegalIdentity[Constants.XmppProperties.MiddleName];
				this.LastNames = this.LegalIdentity[Constants.XmppProperties.LastName];
				this.PersonalNumber = this.LegalIdentity[Constants.XmppProperties.PersonalNumber];
				this.Address = this.LegalIdentity[Constants.XmppProperties.Address];
				this.Address2 = this.LegalIdentity[Constants.XmppProperties.Address2];
				this.ZipCode = this.LegalIdentity[Constants.XmppProperties.ZipCode];
				this.Area = this.LegalIdentity[Constants.XmppProperties.Area];
				this.City = this.LegalIdentity[Constants.XmppProperties.City];
				this.Region = this.LegalIdentity[Constants.XmppProperties.Region];
				this.CountryCode = this.LegalIdentity[Constants.XmppProperties.Country];
				this.PhoneNr = this.LegalIdentity[Constants.XmppProperties.Phone];
			}
			else
			{
				this.FirstName = string.Empty;
				this.MiddleNames = string.Empty;
				this.LastNames = string.Empty;
				this.PersonalNumber = string.Empty;
				this.Address = string.Empty;
				this.Address2 = string.Empty;
				this.ZipCode = string.Empty;
				this.Area = string.Empty;
				this.City = string.Empty;
				this.Region = string.Empty;
				this.CountryCode = string.Empty;
				this.PhoneNr = string.Empty;
			}

			this.Country = ISO_3166_1.ToName(this.CountryCode);
			this.IsApproved = this.LegalIdentity?.State == IdentityState.Approved;
			this.IsCreated = this.LegalIdentity?.State == IdentityState.Created;

			this.IsForReview = !(this.identityToReview is null);
			this.IsNotForReview = !IsForReview;
			this.ThirdParty = !(this.LegalIdentity is null) && !this.IsPersonal;

			this.UpdateSubscriptionStatus();

			this.IsForReviewFirstName = !string.IsNullOrWhiteSpace(this.FirstName) && this.IsForReview;
			this.IsForReviewMiddleNames = !string.IsNullOrWhiteSpace(this.MiddleNames) && this.IsForReview;
			this.IsForReviewLastNames = !string.IsNullOrWhiteSpace(this.LastNames) && this.IsForReview;
			this.IsForReviewPersonalNumber = !string.IsNullOrWhiteSpace(this.PersonalNumber) && this.IsForReview;
			this.IsForReviewAddress = !string.IsNullOrWhiteSpace(this.Address) && this.IsForReview;
			this.IsForReviewAddress2 = !string.IsNullOrWhiteSpace(this.Address2) && this.IsForReview;
			this.IsForReviewCity = !string.IsNullOrWhiteSpace(this.City) && this.IsForReview;
			this.IsForReviewZipCode = !string.IsNullOrWhiteSpace(this.ZipCode) && this.IsForReview;
			this.IsForReviewArea = !string.IsNullOrWhiteSpace(this.Area) && this.IsForReview;
			this.IsForReviewRegion = !string.IsNullOrWhiteSpace(this.Region) && this.IsForReview;
			this.IsForReviewCountry = !string.IsNullOrWhiteSpace(this.Country) && this.IsForReview;

			// QR
			if (this.LegalIdentity is null)
				this.QrCode = null;
			else
			{
				byte[] bytes = Services.UI.QR.QrCode.GeneratePng(Constants.UriSchemes.CreateIdUri(this.LegalIdentity.Id), this.QrCodeWidth, this.QrCodeHeight);
				this.QrCode = ImageSource.FromStream(() => new MemoryStream(bytes));
			}

			if (this.IsConnected)
				this.ReloadPhotos();
		}

		/// <summary>
		/// Copies ID to clipboard
		/// </summary>
		private async void CopyHtmlToClipboard()
		{
			try
			{
				await Clipboard.SetTextAsync($"iotid:{LegalId}");
				await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.IdCopiedSuccessfully);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ApproveCommand, this.RejectCommand, this.RevokeCommand, this.TransferCommand,
				this.ChangePinCommand, this.CompromiseCommand, this.AddContactCommand, this.RemoveContactCommand,
				this.SendPaymentToCommand, this.ChatCommand, this.SubscribeToCommand, this.UnsubscribeFromCommand);
		}

		/// <inheritdoc/>
		protected override void XmppService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				this.SetConnectionStateAndText(e.State);
				this.EvaluateAllCommands();
				if (this.IsConnected)
				{
					await Task.Delay(Constants.Timeouts.XmppInit);
					this.ReloadPhotos();
				}
			});
		}

		private void ReloadPhotos()
		{
			this.photosLoader.CancelLoadPhotos();
			Attachment[] attachments;
			if (!(this.identityToReview?.RequestorIdentity?.Attachments is null))
			{
				attachments = this.identityToReview.RequestorIdentity.Attachments;
			}
			else
			{
				attachments = this.LegalIdentity?.Attachments;
			}
			if (!(attachments is null))
			{
				_ = this.photosLoader.LoadPhotos(attachments, SignWith.LatestApprovedIdOrCurrentKeys);
			}
		}

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(AssignProperties);
		}

		private void SmartContracts_LegalIdentityChanged(object sender, LegalIdentityChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				if (this.LegalIdentity?.Id == e.Identity.Id)
				{
					this.LegalIdentity = e.Identity;
					AssignProperties();
				}
			});
		}

		#region Properties

		/// <summary>
		/// See <see cref="Created"/>
		/// </summary>
		public static readonly BindableProperty CreatedProperty =
			BindableProperty.Create("Created", typeof(DateTime), typeof(ViewIdentityViewModel), default(DateTime));

		/// <summary>
		/// Created time stamp of the identity
		/// </summary>
		public DateTime Created
		{
			get { return (DateTime)GetValue(CreatedProperty); }
			set { SetValue(CreatedProperty, value); }
		}

		/// <summary>
		/// See <see cref="Updated"/>
		/// </summary>
		public static readonly BindableProperty UpdatedProperty =
			BindableProperty.Create("Updated", typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

		/// <summary>
		/// Updated time stamp of the identity
		/// </summary>
		public DateTime? Updated
		{
			get { return (DateTime?)GetValue(UpdatedProperty); }
			set { SetValue(UpdatedProperty, value); }
		}

		/// <summary>
		/// See <see cref="LegalId"/>
		/// </summary>
		public static readonly BindableProperty LegalIdProperty =
			BindableProperty.Create("LegalId", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Legal id of the identity
		/// </summary>
		public string LegalId
		{
			get { return (string)GetValue(LegalIdProperty); }
			set { SetValue(LegalIdProperty, value); }
		}

		/// <summary>
		/// The full legal identity of the identity
		/// </summary>
		public LegalIdentity LegalIdentity { get; private set; }

		/// <summary>
		/// See <see cref="BareJid"/>
		/// </summary>
		public static readonly BindableProperty BareJidProperty =
			BindableProperty.Create("BareJid", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Bare Jid of the identity
		/// </summary>
		public string BareJid
		{
			get { return (string)GetValue(BareJidProperty); }
			set { SetValue(BareJidProperty, value); }
		}

		/// <summary>
		/// See <see cref="PublicKey"/>
		/// </summary>
		public static readonly BindableProperty PublicKeyProperty =
			BindableProperty.Create("PublicKey", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Public key of the identity's signature.
		/// </summary>
		public string PublicKey
		{
			get { return (string)GetValue(PublicKeyProperty); }
			set { SetValue(PublicKeyProperty, value); }
		}

		/// <summary>
		/// See <see cref="State"/>
		/// </summary>
		public static readonly BindableProperty StateProperty =
			BindableProperty.Create("State", typeof(IdentityState), typeof(ViewIdentityViewModel), default(IdentityState));

		/// <summary>
		/// Current state of the identity
		/// </summary>
		public IdentityState State
		{
			get { return (IdentityState)GetValue(StateProperty); }
			set { SetValue(StateProperty, value); }
		}

		/// <summary>
		/// See <see cref="From"/>
		/// </summary>
		public static readonly BindableProperty FromProperty =
			BindableProperty.Create("From", typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

		/// <summary>
		/// From date (validity range) of the identity
		/// </summary>
		public DateTime? From
		{
			get { return (DateTime?)GetValue(FromProperty); }
			set { SetValue(FromProperty, value); }
		}

		/// <summary>
		/// See <see cref="To"/>
		/// </summary>
		public static readonly BindableProperty ToProperty =
			BindableProperty.Create("To", typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

		/// <summary>
		/// To date (validity range) of the identity
		/// </summary>
		public DateTime? To
		{
			get { return (DateTime?)GetValue(ToProperty); }
			set { SetValue(ToProperty, value); }
		}

		/// <summary>
		/// See <see cref="FirstName"/>
		/// </summary>
		public static readonly BindableProperty FirstNameProperty =
			BindableProperty.Create("FirstName", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// First name of the identity
		/// </summary>
		public string FirstName
		{
			get { return (string)GetValue(FirstNameProperty); }
			set { SetValue(FirstNameProperty, value); }
		}

		/// <summary>
		/// See <see cref="MiddleNames"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesProperty =
			BindableProperty.Create("MiddleNames", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Middle name(s) of the identity
		/// </summary>
		public string MiddleNames
		{
			get { return (string)GetValue(MiddleNamesProperty); }
			set { SetValue(MiddleNamesProperty, value); }
		}

		/// <summary>
		/// See <see cref="LastNames"/>
		/// </summary>
		public static readonly BindableProperty LastNamesProperty =
			BindableProperty.Create("LastNames", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Last name(s) of the identity
		/// </summary>
		public string LastNames
		{
			get { return (string)GetValue(LastNamesProperty); }
			set { SetValue(LastNamesProperty, value); }
		}

		/// <summary>
		/// See <see cref="PersonalNumber"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberProperty =
			BindableProperty.Create("PersonalNumber", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Personal number of the identity
		/// </summary>
		public string PersonalNumber
		{
			get { return (string)GetValue(PersonalNumberProperty); }
			set { SetValue(PersonalNumberProperty, value); }
		}

		/// <summary>
		/// See <see cref="Address"/>
		/// </summary>
		public static readonly BindableProperty AddressProperty =
			BindableProperty.Create("Address", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Address of the identity
		/// </summary>
		public string Address
		{
			get { return (string)GetValue(AddressProperty); }
			set { SetValue(AddressProperty, value); }
		}

		/// <summary>
		/// See <see cref="Address2"/>
		/// </summary>
		public static readonly BindableProperty Address2Property =
			BindableProperty.Create("Address2", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Address (line 2) of the identity
		/// </summary>
		public string Address2
		{
			get { return (string)GetValue(Address2Property); }
			set { SetValue(Address2Property, value); }
		}

		/// <summary>
		/// See <see cref="ZipCode"/>
		/// </summary>
		public static readonly BindableProperty ZipCodeProperty =
			BindableProperty.Create("ZipCode", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Zip code of the identity
		/// </summary>
		public string ZipCode
		{
			get { return (string)GetValue(ZipCodeProperty); }
			set { SetValue(ZipCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="Area"/>
		/// </summary>
		public static readonly BindableProperty AreaProperty =
			BindableProperty.Create("Area", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Area of the identity
		/// </summary>
		public string Area
		{
			get { return (string)GetValue(AreaProperty); }
			set { SetValue(AreaProperty, value); }
		}

		/// <summary>
		/// See <see cref="City"/>
		/// </summary>
		public static readonly BindableProperty CityProperty =
			BindableProperty.Create("City", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// City of the identity
		/// </summary>
		public string City
		{
			get { return (string)GetValue(CityProperty); }
			set { SetValue(CityProperty, value); }
		}

		/// <summary>
		/// See <see cref="Region"/>
		/// </summary>
		public static readonly BindableProperty RegionProperty =
			BindableProperty.Create("Region", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Region of the identity
		/// </summary>
		public string Region
		{
			get { return (string)GetValue(RegionProperty); }
			set { SetValue(RegionProperty, value); }
		}

		/// <summary>
		/// See <see cref="Country"/>
		/// </summary>
		public static readonly BindableProperty CountryProperty =
			BindableProperty.Create("Country", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Country of the identity
		/// </summary>
		public string Country
		{
			get { return (string)GetValue(CountryProperty); }
			set { SetValue(CountryProperty, value); }
		}

		/// <summary>
		/// See <see cref="CountryCode"/>
		/// </summary>
		public static readonly BindableProperty CountryCodeProperty =
			BindableProperty.Create("CountryCode", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Country code of the identity
		/// </summary>
		public string CountryCode
		{
			get { return (string)GetValue(CountryCodeProperty); }
			set { SetValue(CountryCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="PhoneNr"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrProperty =
			BindableProperty.Create("PhoneNr", typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Country code of the identity
		/// </summary>
		public string PhoneNr
		{
			get { return (string)GetValue(PhoneNrProperty); }
			set { SetValue(PhoneNrProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsApproved"/>
		/// </summary>
		public static readonly BindableProperty IsApprovedProperty =
			BindableProperty.Create("IsApproved", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// See <see cref="QrCodeProperty"/>
		/// </summary>
		public static readonly BindableProperty QrCodeProperty =
			BindableProperty.Create("QrCode", typeof(ImageSource), typeof(ViewIdentityViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				ViewIdentityViewModel viewModel = (ViewIdentityViewModel)b;
				viewModel.HasQrCode = !(newValue is null);
			});

		/// <summary>
		/// Generated QR code image for the identity
		/// </summary>
		public ImageSource QrCode
		{
			get { return (ImageSource)GetValue(QrCodeProperty); }
			set { SetValue(QrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasQrCode"/>
		/// </summary>
		public static readonly BindableProperty HasQrCodeProperty =
			BindableProperty.Create("HasQrCode", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Determines whether there's a generated <see cref="QrCode"/> image for this identity.
		/// </summary>
		public bool HasQrCode
		{
			get { return (bool)GetValue(HasQrCodeProperty); }
			set { SetValue(HasQrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeWidth"/>
		/// </summary>
		public static readonly BindableProperty QrCodeWidthProperty =
			BindableProperty.Create("QrCodeWidth", typeof(int), typeof(ViewIdentityViewModel), UiConstants.QrCode.DefaultImageWidth);

		/// <summary>
		/// Gets or sets the width, in pixels, of the QR Code image to generate.
		/// </summary>
		public int QrCodeWidth
		{
			get { return (int)GetValue(QrCodeWidthProperty); }
			set { SetValue(QrCodeWidthProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeHeight"/>
		/// </summary>
		public static readonly BindableProperty QrCodeHeightProperty =
			BindableProperty.Create("QrCodeHeight", typeof(int), typeof(ViewIdentityViewModel), UiConstants.QrCode.DefaultImageHeight);

		/// <summary>
		/// Gets or sets the height, in pixels, of the QR Code image to generate.
		/// </summary>
		public int QrCodeHeight
		{
			get { return (int)GetValue(QrCodeHeightProperty); }
			set { SetValue(QrCodeHeightProperty, value); }
		}

		/// <summary>
		/// Gets or sets whether the identity is approved or not.
		/// </summary>
		public bool IsApproved
		{
			get { return (bool)GetValue(IsApprovedProperty); }
			set { SetValue(IsApprovedProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsCreated"/>
		/// </summary>
		public static readonly BindableProperty IsCreatedProperty =
			BindableProperty.Create("IsCreated", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets created state of the identity, i.e. if it has been created or not.
		/// </summary>
		public bool IsCreated
		{
			get { return (bool)GetValue(IsCreatedProperty); }
			set { SetValue(IsCreatedProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReview"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewProperty =
			BindableProperty.Create("IsForReview", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsNotForReview"/>.
		/// </summary>
		public bool IsForReview
		{
			get { return (bool)GetValue(IsForReviewProperty); }
			set { SetValue(IsForReviewProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsNotForReview"/>
		/// </summary>
		public static readonly BindableProperty IsNotForReviewProperty =
			BindableProperty.Create("IsNotForReview", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
		/// </summary>
		public bool IsNotForReview
		{
			get { return (bool)GetValue(IsNotForReviewProperty); }
			set { SetValue(IsNotForReviewProperty, value); }
		}

		/// <summary>
		/// See <see cref="ThirdParty"/>
		/// </summary>
		public static readonly BindableProperty ThirdPartyProperty =
			BindableProperty.Create("ThirdParty", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
		/// </summary>
		public bool ThirdParty
		{
			get { return (bool)GetValue(ThirdPartyProperty); }
			set { SetValue(ThirdPartyProperty, value); }
		}

		/// <summary>
		/// See <see cref="ThirdPartyInContacts"/>
		/// </summary>
		public static readonly BindableProperty ThirdPartyInContactsProperty =
			BindableProperty.Create("ThirdPartyInContacts", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
		/// </summary>
		public bool ThirdPartyInContacts
		{
			get { return (bool)GetValue(ThirdPartyInContactsProperty); }
			set { SetValue(ThirdPartyInContactsProperty, value); }
		}

		/// <summary>
		/// See <see cref="ThirdPartyNotInContacts"/>
		/// </summary>
		public static readonly BindableProperty ThirdPartyNotInContactsProperty =
			BindableProperty.Create("ThirdPartyNotInContacts", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
		/// </summary>
		public bool ThirdPartyNotInContacts
		{
			get { return (bool)GetValue(ThirdPartyNotInContactsProperty); }
			set { SetValue(ThirdPartyNotInContactsProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsPersonal"/>
		/// </summary>
		public static readonly BindableProperty IsPersonalProperty =
			BindableProperty.Create("IsPersonal", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is a personal identity.
		/// </summary>
		public bool IsPersonal
		{
			get { return (bool)GetValue(IsPersonalProperty); }
			set { SetValue(IsPersonalProperty, value); }
		}

		/// <summary>
		/// See <see cref="Subscribed"/>
		/// </summary>
		public static readonly BindableProperty SubscribedProperty =
			BindableProperty.Create("Subscribed", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
		/// </summary>
		public bool Subscribed
		{
			get { return (bool)GetValue(SubscribedProperty); }
			set { SetValue(SubscribedProperty, value); }
		}

		/// <summary>
		/// See <see cref="NotSubscribed"/>
		/// </summary>
		public static readonly BindableProperty NotSubscribedProperty =
			BindableProperty.Create("NotSubscribed", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
		/// </summary>
		public bool NotSubscribed
		{
			get { return (bool)GetValue(NotSubscribedProperty); }
			set { SetValue(NotSubscribedProperty, value); }
		}

		/// <summary>
		/// See <see cref="FirstNameIsChecked"/>
		/// </summary>
		public static readonly BindableProperty FirstNameIsCheckedProperty =
			BindableProperty.Create("FirstNameIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="FirstName"/> property is checked (when being reviewed)
		/// </summary>
		public bool FirstNameIsChecked
		{
			get { return (bool)GetValue(FirstNameIsCheckedProperty); }
			set { SetValue(FirstNameIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="MiddleNamesIsChecked"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesIsCheckedProperty =
			BindableProperty.Create("MiddleNamesIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="MiddleNames"/> property is checked (when being reviewed)
		/// </summary>
		public bool MiddleNamesIsChecked
		{
			get { return (bool)GetValue(MiddleNamesIsCheckedProperty); }
			set { SetValue(MiddleNamesIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="LastNamesIsChecked"/>
		/// </summary>
		public static readonly BindableProperty LastNamesIsCheckedProperty =
			BindableProperty.Create("LastNamesIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="LastNames"/> property is checked (when being reviewed)
		/// </summary>
		public bool LastNamesIsChecked
		{
			get { return (bool)GetValue(LastNamesIsCheckedProperty); }
			set { SetValue(LastNamesIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="PersonalNumberIsChecked"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberIsCheckedProperty =
			BindableProperty.Create("PersonalNumberIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="PersonalNumber"/> property is checked (when being reviewed)
		/// </summary>
		public bool PersonalNumberIsChecked
		{
			get { return (bool)GetValue(PersonalNumberIsCheckedProperty); }
			set { SetValue(PersonalNumberIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="AddressIsChecked"/>
		/// </summary>
		public static readonly BindableProperty AddressIsCheckedProperty =
			BindableProperty.Create("AddressIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Address"/> property is checked (when being reviewed)
		/// </summary>
		public bool AddressIsChecked
		{
			get { return (bool)GetValue(AddressIsCheckedProperty); }
			set { SetValue(AddressIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="AddressIsChecked"/>
		/// </summary>
		public static readonly BindableProperty Address2IsCheckedProperty =
			BindableProperty.Create("Address2IsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Address2"/> property is checked (when being reviewed)
		/// </summary>
		public bool Address2IsChecked
		{
			get { return (bool)GetValue(Address2IsCheckedProperty); }
			set { SetValue(Address2IsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="ZipCodeIsChecked"/>
		/// </summary>
		public static readonly BindableProperty ZipCodeIsCheckedProperty =
			BindableProperty.Create("ZipCodeIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="ZipCode"/> property is checked (when being reviewed)
		/// </summary>
		public bool ZipCodeIsChecked
		{
			get { return (bool)GetValue(ZipCodeIsCheckedProperty); }
			set { SetValue(ZipCodeIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="AreaIsChecked"/>
		/// </summary>
		public static readonly BindableProperty AreaIsCheckedProperty =
			BindableProperty.Create("AreaIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Area"/> property is checked (when being reviewed)
		/// </summary>
		public bool AreaIsChecked
		{
			get { return (bool)GetValue(AreaIsCheckedProperty); }
			set { SetValue(AreaIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="CityIsChecked"/>
		/// </summary>
		public static readonly BindableProperty CityIsCheckedProperty =
			BindableProperty.Create("CityIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="City"/> property is checked (when being reviewed)
		/// </summary>
		public bool CityIsChecked
		{
			get { return (bool)GetValue(CityIsCheckedProperty); }
			set { SetValue(CityIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="RegionIsChecked"/>
		/// </summary>
		public static readonly BindableProperty RegionIsCheckedProperty =
			BindableProperty.Create("RegionIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Region"/> property is checked (when being reviewed)
		/// </summary>
		public bool RegionIsChecked
		{
			get { return (bool)GetValue(RegionIsCheckedProperty); }
			set { SetValue(RegionIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="CountryCodeIsChecked"/>
		/// </summary>
		public static readonly BindableProperty CountryCodeIsCheckedProperty =
			BindableProperty.Create("CountryCodeIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="CountryCode"/> property is checked (when being reviewed)
		/// </summary>
		public bool CountryCodeIsChecked
		{
			get { return (bool)GetValue(CountryCodeIsCheckedProperty); }
			set { SetValue(CountryCodeIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="CarefulReviewIsChecked"/>
		/// </summary>
		public static readonly BindableProperty CarefulReviewIsCheckedProperty =
			BindableProperty.Create("CarefulReviewIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the Careful Review property is checked (when being reviewed)
		/// </summary>
		public bool CarefulReviewIsChecked
		{
			get { return (bool)GetValue(CarefulReviewIsCheckedProperty); }
			set { SetValue(CarefulReviewIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="ApprovePiiIsChecked"/>
		/// </summary>
		public static readonly BindableProperty ApprovePiiIsCheckedProperty =
			BindableProperty.Create("ApprovePiiIsChecked", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the ApprovePii property is checked (when being reviewed)
		/// </summary>
		public bool ApprovePiiIsChecked
		{
			get { return (bool)GetValue(ApprovePiiIsCheckedProperty); }
			set { SetValue(ApprovePiiIsCheckedProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewFirstName"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewFirstNameProperty =
			BindableProperty.Create("IsForReviewFirstName", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="FirstName"/> property is for review.
		/// </summary>
		public bool IsForReviewFirstName
		{
			get { return (bool)GetValue(IsForReviewFirstNameProperty); }
			set { SetValue(IsForReviewFirstNameProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewMiddleNames"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewMiddleNamesProperty =
			BindableProperty.Create("IsForReviewMiddleNames", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="MiddleNames"/> property is for review.
		/// </summary>
		public bool IsForReviewMiddleNames
		{
			get { return (bool)GetValue(IsForReviewMiddleNamesProperty); }
			set { SetValue(IsForReviewMiddleNamesProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewLastNames"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewLastNamesProperty =
			BindableProperty.Create("IsForReviewLastNames", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="LastNames"/> property is for review.
		/// </summary>
		public bool IsForReviewLastNames
		{
			get { return (bool)GetValue(IsForReviewLastNamesProperty); }
			set { SetValue(IsForReviewLastNamesProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewPersonalNumber"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewPersonalNumberProperty =
			BindableProperty.Create("IsForReviewPersonalNumber", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="PersonalNumber"/> property is for review.
		/// </summary>
		public bool IsForReviewPersonalNumber
		{
			get { return (bool)GetValue(IsForReviewPersonalNumberProperty); }
			set { SetValue(IsForReviewPersonalNumberProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewAddress"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewAddressProperty =
			BindableProperty.Create("IsForReviewAddress", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Address"/> property is for review.
		/// </summary>
		public bool IsForReviewAddress
		{
			get { return (bool)GetValue(IsForReviewAddressProperty); }
			set { SetValue(IsForReviewAddressProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewAddress2"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewAddress2Property =
			BindableProperty.Create("IsForReviewAddress2", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Address2"/> property is for review.
		/// </summary>
		public bool IsForReviewAddress2
		{
			get { return (bool)GetValue(IsForReviewAddress2Property); }
			set { SetValue(IsForReviewAddress2Property, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewCity"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewCityProperty =
			BindableProperty.Create("IsForReviewCity", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="City"/> property is for review.
		/// </summary>
		public bool IsForReviewCity
		{
			get { return (bool)GetValue(IsForReviewCityProperty); }
			set { SetValue(IsForReviewCityProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewZipCode"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewZipCodeProperty =
			BindableProperty.Create("IsForReviewZipCode", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="ZipCode"/> property is for review.
		/// </summary>
		public bool IsForReviewZipCode
		{
			get { return (bool)GetValue(IsForReviewZipCodeProperty); }
			set { SetValue(IsForReviewZipCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewArea"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewAreaProperty =
			BindableProperty.Create("IsForReviewArea", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Area"/> property is for review.
		/// </summary>
		public bool IsForReviewArea
		{
			get { return (bool)GetValue(IsForReviewAreaProperty); }
			set { SetValue(IsForReviewAreaProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewRegion"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewRegionProperty =
			BindableProperty.Create("IsForReviewRegion", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Region"/> property is for review.
		/// </summary>
		public bool IsForReviewRegion
		{
			get { return (bool)GetValue(IsForReviewRegionProperty); }
			set { SetValue(IsForReviewRegionProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsForReviewCountry"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewCountryProperty =
			BindableProperty.Create("IsForReviewCountry", typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Country"/> property is for review.
		/// </summary>
		public bool IsForReviewCountry
		{
			get { return (bool)GetValue(IsForReviewCountryProperty); }
			set { SetValue(IsForReviewCountryProperty, value); }
		}

		#endregion

		private async Task Approve()
		{
			if (this.identityToReview is null)
				return;

			try
			{
				if ((!string.IsNullOrEmpty(this.FirstName) && !this.FirstNameIsChecked) ||
					(!string.IsNullOrEmpty(this.MiddleNames) && !this.MiddleNamesIsChecked) ||
					(!string.IsNullOrEmpty(this.LastNames) && !this.LastNamesIsChecked) ||
					(!string.IsNullOrEmpty(this.PersonalNumber) && !this.PersonalNumberIsChecked) ||
					(!string.IsNullOrEmpty(this.Address) && !this.AddressIsChecked) ||
					(!string.IsNullOrEmpty(this.Address2) && !this.Address2IsChecked) ||
					(!string.IsNullOrEmpty(this.ZipCode) && !this.ZipCodeIsChecked) ||
					(!string.IsNullOrEmpty(this.Area) && !this.AreaIsChecked) ||
					(!string.IsNullOrEmpty(this.City) && !this.CityIsChecked) ||
					(!string.IsNullOrEmpty(this.Region) && !this.RegionIsChecked) ||
					(!string.IsNullOrEmpty(this.CountryCode) && !this.CountryCodeIsChecked))
				{
					await this.UiSerializer.DisplayAlert(AppResources.Incomplete, AppResources.PleaseReviewAndCheckAllCheckboxes);
					return;
				}

				if (!this.CarefulReviewIsChecked)
				{
					await this.UiSerializer.DisplayAlert(AppResources.Incomplete, AppResources.YouNeedToCheckCarefullyReviewed);
					return;
				}

				if (!this.ApprovePiiIsChecked)
				{
					await this.UiSerializer.DisplayAlert(AppResources.Incomplete, AppResources.YouNeedToApproveToAssociate);
					return;
				}

				if (!await App.VerifyPin())
					return;

				(bool succeeded1, byte[] signature) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.Sign(this.identityToReview.ContentToSign, SignWith.LatestApprovedId));

				if (!succeeded1)
				{
					return;
				}

				bool succeeded2 = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionSignatureResponse(this.identityToReview.SignatoryIdentityId, this.identityToReview.ContentToSign, signature, this.identityToReview.PetitionId, this.identityToReview.RequestorFullJid, true));

				if (succeeded2)
				{
					await this.NavigationService.GoBackAsync();
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Reject()
		{
			if (this.identityToReview is null)
				return;

			try
			{
				bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.SendPetitionSignatureResponse(this.identityToReview.SignatoryIdentityId, this.identityToReview.ContentToSign, new byte[0], this.identityToReview.PetitionId, this.identityToReview.RequestorFullJid, false));
				if (succeeded)
				{
					await this.NavigationService.GoBackAsync();
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Revoke()
		{
			if (!this.IsPersonal)
				return;

			try
			{
				if (!await this.AreYouSure(AppResources.AreYouSureYouWantToRevokeYourLegalIdentity))
					return;

				(bool succeeded, LegalIdentity revokedIdentity) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.ObsoleteLegalIdentity(this.LegalIdentity.Id));
				if (succeeded)
				{
					this.LegalIdentity = revokedIdentity;
					this.TagProfile.RevokeLegalIdentity(revokedIdentity);

					await this.XmppService.Contracts.ContractsClient.GenerateNewKeys();
					await this.NavigationService.GoToAsync($"{nameof(RegistrationPage)}");
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task<bool> AreYouSure(string Message)
		{
			if (!await App.VerifyPin())
				return false;

			return await this.UiSerializer.DisplayAlert(AppResources.Confirm, Message, AppResources.Yes, AppResources.No);
		}

		private async Task Compromise()
		{
			if (!this.IsPersonal)
				return;

			try
			{
				if (!await this.AreYouSure(AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized))
					return;

				(bool succeeded, LegalIdentity compromisedIdentity) = await this.NetworkService.TryRequest(() => this.XmppService.Contracts.CompromiseLegalIdentity(this.LegalIdentity.Id));

				if (succeeded)
				{
					this.LegalIdentity = compromisedIdentity;
					this.TagProfile.RevokeLegalIdentity(compromisedIdentity);

					await this.XmppService.Contracts.ContractsClient.GenerateNewKeys();
					await this.NavigationService.GoToAsync($"{nameof(RegistrationPage)}");
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private string GetFriendlyName()
		{
			StringBuilder Name = new StringBuilder();
			string s;

			Name.Append(this.FirstName);

			s = this.MiddleNames;
			if (!string.IsNullOrEmpty(s))
			{
				Name.Append(' ');
				Name.Append(s);
			}

			s = this.LastNames;
			if (!string.IsNullOrEmpty(s))
			{
				Name.Append(' ');
				Name.Append(s);
			}

			return Name.ToString();
		}

		private async Task AddContact()
		{
			try
			{
				string FriendlyName = this.GetFriendlyName();

				RosterItem Item = this.XmppService.Xmpp[this.BareJid];
				if (Item is null)
					this.XmppService.Xmpp.AddRosterItem(new RosterItem(this.BareJid, FriendlyName));

				ContactInfo Info = await ContactInfo.FindByBareJid(this.BareJid);
				if (Info is null)
				{
					Info = new ContactInfo()
					{
						BareJid = this.BareJid,
						LegalId = this.LegalId,
						LegalIdentity = this.LegalIdentity,
						FriendlyName = FriendlyName,
						IsThing = false
					};

					await Database.Insert(Info);
				}
				else
				{
					Info.LegalId = this.LegalId;
					Info.LegalIdentity = this.LegalIdentity;
					Info.FriendlyName = FriendlyName;

					await Database.Update(Info);
				}

				await this.AttachmentCacheService.MakePermanent(this.LegalId);
				await Database.Provider.Flush();

				this.ThirdPartyInContacts = true;
				this.ThirdPartyNotInContacts = false;

				this.EvaluateAllCommands();
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task RemoveContact()
		{
			try
			{
				if (!await this.UiSerializer.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToRemoveContact, AppResources.Yes, AppResources.Cancel))
					return;

				ContactInfo Info = await ContactInfo.FindByBareJid(this.BareJid);
				if (!(Info is null))
				{
					await Database.Delete(Info);
					await this.AttachmentCacheService.MakeTemporary(Info.LegalId);
					await Database.Provider.Flush();
				}

				RosterItem Item = this.XmppService.Xmpp[this.BareJid];
				if (!(Item is null))
					this.XmppService.Xmpp.RemoveRosterItem(this.BareJid);

				this.ThirdPartyInContacts = false;
				this.ThirdPartyNotInContacts = true;

				this.EvaluateAllCommands();
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task SendPaymentTo()
		{
			try
			{
				Balance Balance = await this.XmppService.Wallet.GetBalanceAsync();
				string Uri;

				if (this.LegalIdentity is null)
					Uri = this.XmppService.Wallet.CreateIncompletePayMeUri(this.BareJid, null, null, Balance.Currency, string.Empty);
				else
					Uri = this.XmppService.Wallet.CreateIncompletePayMeUri(this.LegalIdentity, null, null, Balance.Currency, string.Empty);

				await this.EDalerOrchestratorService.OpenEDalerUri(Uri);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Transfer()
		{
			if (!this.IsPersonal)
				return;

			try
			{
				string Pin = await App.InputPin();
				if (Pin is null)
					return;

				if (!await this.UiSerializer.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToTransferYourLegalIdentity, AppResources.Yes, AppResources.No))
					return;

				this.IsBusy = true;
				this.EvaluateAllCommands();
				try
				{
					StringBuilder Xml = new StringBuilder();
					XmlWriterSettings Settings = XML.WriterSettings(false, true);

					using (XmlWriter Output = XmlWriter.Create(Xml, Settings))
					{
						Output.WriteStartElement("Transfer", ContractsClient.NamespaceOnboarding);

						await this.XmppService.Contracts.ContractsClient.ExportKeys(Output);

						Output.WriteStartElement("Pin");
						Output.WriteAttributeString("pin", Pin);
						Output.WriteEndElement();

						Output.WriteStartElement("Account", ContractsClient.NamespaceOnboarding);
						Output.WriteAttributeString("domain", this.TagProfile.Domain);
						Output.WriteAttributeString("userName", this.TagProfile.Account);
						Output.WriteAttributeString("password", this.TagProfile.PasswordHash);

						if (!string.IsNullOrEmpty(this.TagProfile.PasswordHashMethod))
							Output.WriteAttributeString("passwordMethod", this.TagProfile.PasswordHashMethod);

						Output.WriteEndElement();
						Output.WriteEndElement();
					}

					using (RandomNumberGenerator Rnd = RandomNumberGenerator.Create())
					{
						byte[] Data = Encoding.UTF8.GetBytes(Xml.ToString());
						byte[] Key = new byte[16];
						byte[] IV = new byte[16];

						Rnd.GetBytes(Key);
						Rnd.GetBytes(IV);

						using (Aes Aes = Aes.Create())
						{
							Aes.BlockSize = 128;
							Aes.KeySize = 256;
							Aes.Mode = CipherMode.CBC;
							Aes.Padding = PaddingMode.PKCS7;

							using (ICryptoTransform Transform = Aes.CreateEncryptor(Key, IV))
							{
								byte[] Encrypted = Transform.TransformFinalBlock(Data, 0, Data.Length);

								Xml.Clear();

								using (XmlWriter Output = XmlWriter.Create(Xml, Settings))
								{
									Output.WriteStartElement("Info", ContractsClient.NamespaceOnboarding);
									Output.WriteAttributeString("base64", Convert.ToBase64String(Encrypted));
									Output.WriteAttributeString("once", "true");
									Output.WriteAttributeString("expires", XML.Encode(DateTime.UtcNow.AddMinutes(1)));
									Output.WriteEndElement();
								}

								XmlElement Response = await this.XmppService.Xmpp.IqSetAsync(Constants.Domains.OnboardingDomain, Xml.ToString());

								foreach (XmlNode N in Response.ChildNodes)
								{
									if (N is XmlElement Info && Info.LocalName == "Code" && Info.NamespaceURI == ContractsClient.NamespaceOnboarding)
									{
										string Code = XML.Attribute(Info, "code");
										string Url = "obinfo:" + Constants.Domains.IdDomain + ":" + Code + ":" +
											Convert.ToBase64String(Key) + ":" + Convert.ToBase64String(IV);

										await this.XmppService.AddTransferCode(Code);
										await this.NavigationService.GoToAsync(nameof(TransferIdentityPage), new TransferIdentityNavigationArgs(Url));
										return;
									}
								}

								await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnexpectedResponse);
							}
						}
					}
				}
				finally
				{
					this.IsBusy = false;
					this.EvaluateAllCommands();
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ChangePin()
		{
			if (!this.IsPersonal)
				return;

			try
			{
				while (true)
				{
					ChangePinPopupPage Page = new ChangePinPopupPage();

					await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(Page);
					(string OldPin, string NewPin) = await Page.Result;

					if (OldPin is null || OldPin == NewPin)
						return;

					if (this.TagProfile.ComputePinHash(OldPin) == this.TagProfile.PinHash)
					{
						TaskCompletionSource<bool> PasswordChanged = new TaskCompletionSource<bool>();
						string NewPassword = this.CryptoService.CreateRandomPassword();

						this.XmppService.Xmpp.ChangePassword(NewPassword, (sender, e) =>
						{
							PasswordChanged.TrySetResult(e.Ok);
							return Task.CompletedTask;
						}, null);

						if (!await PasswordChanged.Task)
						{
							await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToChangePassword);
							return;
						}

						this.TagProfile.Pin = NewPin;
						this.TagProfile.SetAccount(this.TagProfile.Account, NewPassword, string.Empty);

						await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.PinChanged);
						return;
					}

					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);

					// TODO: Limit number of attempts.
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task OpenChat()
		{
			try
			{
				await this.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(this.LegalId, this.BareJid, this.GetFriendlyName()));
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task SubscribeTo()
		{
			if (this.ThirdPartyNotInContacts)
				await this.AddContact();

			try
			{
				string IdXml;

				if (this.TagProfile.LegalIdentity is null)
					IdXml = string.Empty;
				else
				{
					StringBuilder Xml = new StringBuilder();
					this.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
					IdXml = Xml.ToString();
				}

				this.XmppService.Xmpp.RequestPresenceSubscription(this.BareJid, IdXml);
				await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.PresenceSubscriptionRequestSent);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task UnsubscribeFrom()
		{
			try
			{
				this.XmppService.Xmpp.RequestPresenceUnsubscription(this.BareJid);

				RosterItem Item = this.XmppService.Xmpp[this.BareJid];
				if (Item.State == SubscriptionState.Both || Item.State == SubscriptionState.From)
				{
					RemoveSubscriptionPopupPage Page = new RemoveSubscriptionPopupPage(this.BareJid);

					await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(Page);
					bool? Remove = await Page.Result;

					if (Remove.HasValue && Remove.Value)
					{
						this.XmppService.Xmpp.RequestRevokePresenceSubscription(this.BareJid);

						ContactInfo Info = await ContactInfo.FindByBareJid(this.BareJid);
						if (!(Info is null) && Info.AllowSubscriptionFrom.HasValue && Info.AllowSubscriptionFrom.Value)
						{
							Info.AllowSubscriptionFrom = null;
							await Database.Update(Info);
						}
					}
				}

				await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.PresenceUnsubscriptionRequestSent);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private Task CheckRosterItem(object Sender, RosterItem Item)
		{
			if (string.Compare(Item.BareJid, this.BareJid, true) == 0)
				this.UiSerializer.BeginInvokeOnMainThread(() => this.UpdateSubscriptionStatus());

			return Task.CompletedTask;
		}

		private void UpdateSubscriptionStatus()
		{
			RosterItem Item = this.XmppService.Xmpp[this.BareJid];

			this.Subscribed = this.ThirdParty && !(Item is null) && (Item.State == SubscriptionState.Both || Item.State == SubscriptionState.To);
			this.NotSubscribed = this.ThirdParty && (Item is null || (Item.State != SubscriptionState.Both && Item.State != SubscriptionState.To));

			this.EvaluateAllCommands();
		}

	}
}