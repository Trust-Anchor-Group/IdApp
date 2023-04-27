using IdApp.Extensions;
using IdApp.Pages.Main.ScanQrCode;
using IdApp.Pages.Wallet.ServiceProviders;
using IdApp.Services.Data.Countries;
using IdApp.Services.Navigation;
using IdApp.Services.Tag;
using IdApp.Services.UI.Photos;
using IdApp.Services.UI.QR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Registration.ValidateIdentity
{
	/// <summary>
	/// The view model to bind to when showing Step 4 of the registration flow: validating an identity.
	/// </summary>
	public class ValidateIdentityViewModel : RegistrationStepViewModel
	{
		private readonly PhotosLoader photosLoader;
		private readonly SemaphoreSlim reloadPhotosSemaphore = new(1, 1);
		private ServiceProviderWithLegalId[] peerReviewServices = null;

		/// <summary>
		/// Creates a new instance of the <see cref="ValidateIdentityViewModel"/> class.
		/// </summary>
		public ValidateIdentityViewModel()
			: base(RegistrationStep.ValidateIdentity)
		{
			this.RequestReviewCommand = new Command(async _ => await this.RequestReview(), _ => this.State == IdentityState.Created && this.XmppService.IsOnline);
			this.ContinueCommand = new Command(_ => this.Continue(), _ => this.IsApproved);
			this.Title = LocalizationResourceManager.Current["ValidatingInformation"];
			this.Photos = new ObservableCollection<Photo>();
			this.photosLoader = new PhotosLoader(this.Photos);
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			this.AssignProperties();

			this.TagProfile.Changed += this.TagProfile_Changed;
			this.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
			this.XmppService.LegalIdentityChanged += this.XmppContracts_LegalIdentityChanged;

			this.peerReviewServices ??= await this.XmppService.GetServiceProvidersForPeerReviewAsync();
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			this.photosLoader.CancelLoadPhotos();
			this.TagProfile.Changed -= this.TagProfile_Changed;
			this.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
			this.XmppService.LegalIdentityChanged -= this.XmppContracts_LegalIdentityChanged;
			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// The list of photos associated with this legal identity.
		/// </summary>
		public ObservableCollection<Photo> Photos { get; }

		/// <summary>
		/// The command to bind to for requesting a review of the user's identity.
		/// </summary>
		public ICommand RequestReviewCommand { get; }

		/// <summary>
		/// The command to bind to for continuing to the next step in the registration process.
		/// </summary>
		public ICommand ContinueCommand { get; }

		/// <summary>
		/// The <see cref="Created"/>
		/// </summary>
		public static readonly BindableProperty CreatedProperty =
			BindableProperty.Create(nameof(Created), typeof(DateTime), typeof(ValidateIdentityViewModel), default(DateTime));

		/// <summary>
		/// Gets or sets the Created time stamp of the legal identity.
		/// </summary>
		public DateTime Created
		{
			get => (DateTime)this.GetValue(CreatedProperty);
			set => this.SetValue(CreatedProperty, value);
		}

		/// <summary>
		/// The <see cref="Updated"/>
		/// </summary>
		public static readonly BindableProperty UpdatedProperty =
			BindableProperty.Create(nameof(Updated), typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

		/// <summary>
		/// Gets or sets the Updated time stamp of the legal identity.
		/// </summary>
		public DateTime? Updated
		{
			get { return (DateTime?)this.GetValue(UpdatedProperty); }
			set => this.SetValue(UpdatedProperty, value);
		}

		/// <summary>
		/// The <see cref="LegalId"/>
		/// </summary>
		public static readonly BindableProperty LegalIdProperty =
			BindableProperty.Create(nameof(LegalId), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// Gets or sets the Id of the legal identity.
		/// </summary>
		public string LegalId
		{
			get => (string)this.GetValue(LegalIdProperty);
			set => this.SetValue(LegalIdProperty, value);
		}

		/// <summary>
		/// Gets or sets the legal identity.
		/// </summary>
		public LegalIdentity LegalIdentity { get; private set; }

		/// <summary>
		/// The <see cref="BareJid"/>
		/// </summary>
		public static readonly BindableProperty BareJidProperty =
			BindableProperty.Create(nameof(BareJid), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// Gets or sets the Bare Jid registered with the XMPP server.
		/// </summary>
		public string BareJid
		{
			get => (string)this.GetValue(BareJidProperty);
			set => this.SetValue(BareJidProperty, value);
		}

		/// <summary>
		/// The <see cref="State"/>
		/// </summary>
		public static readonly BindableProperty StateProperty =
			BindableProperty.Create(nameof(State), typeof(IdentityState), typeof(ValidateIdentityViewModel), default(IdentityState));

		/// <summary>
		/// The current state of the user's legal identity.
		/// </summary>
		public IdentityState State
		{
			get => (IdentityState)this.GetValue(StateProperty);
			set => this.SetValue(StateProperty, value);
		}

		/// <summary>
		/// The <see cref="From"/>
		/// </summary>
		public static readonly BindableProperty FromProperty =
			BindableProperty.Create(nameof(From), typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

		/// <summary>
		/// Gets or sets the From time stamp (validity range) of the user's identity.
		/// </summary>
		public DateTime? From
		{
			get { return (DateTime?)this.GetValue(FromProperty); }
			set => this.SetValue(FromProperty, value);
		}

		/// <summary>
		/// The <see cref="To"/>
		/// </summary>
		public static readonly BindableProperty ToProperty =
			BindableProperty.Create(nameof(To), typeof(DateTime?), typeof(ValidateIdentityViewModel), default(DateTime?));

		/// <summary>
		/// Gets or sets the To time stamp (validity range) of the user's identity.
		/// </summary>
		public DateTime? To
		{
			get { return (DateTime?)this.GetValue(ToProperty); }
			set => this.SetValue(ToProperty, value);
		}

		/// <summary>
		/// The <see cref="FirstName"/>
		/// </summary>
		public static readonly BindableProperty FirstNameProperty =
			BindableProperty.Create(nameof(FirstName), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's first name
		/// </summary>
		public string FirstName
		{
			get => (string)this.GetValue(FirstNameProperty);
			set => this.SetValue(FirstNameProperty, value);
		}

		/// <summary>
		/// The <see cref="MiddleNames"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesProperty =
			BindableProperty.Create(nameof(MiddleNames), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's middle name(s)
		/// </summary>
		public string MiddleNames
		{
			get => (string)this.GetValue(MiddleNamesProperty);
			set => this.SetValue(MiddleNamesProperty, value);
		}

		/// <summary>
		/// The <see cref="LastNames"/>
		/// </summary>
		public static readonly BindableProperty LastNamesProperty =
			BindableProperty.Create(nameof(LastNames), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's last name(s)
		/// </summary>
		public string LastNames
		{
			get => (string)this.GetValue(LastNamesProperty);
			set => this.SetValue(LastNamesProperty, value);
		}

		/// <summary>
		/// The <see cref="PersonalNumber"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberProperty =
			BindableProperty.Create(nameof(PersonalNumber), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's personal number
		/// </summary>
		public string PersonalNumber
		{
			get => (string)this.GetValue(PersonalNumberProperty);
			set => this.SetValue(PersonalNumberProperty, value);
		}

		/// <summary>
		/// The <see cref="Address"/>
		/// </summary>
		public static readonly BindableProperty AddressProperty =
			BindableProperty.Create(nameof(Address), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's address, line 1.
		/// </summary>
		public string Address
		{
			get => (string)this.GetValue(AddressProperty);
			set => this.SetValue(AddressProperty, value);
		}

		/// <summary>
		/// The <see cref="Address2"/>
		/// </summary>
		public static readonly BindableProperty Address2Property =
			BindableProperty.Create(nameof(Address2), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's address, line 2.
		/// </summary>
		public string Address2
		{
			get => (string)this.GetValue(Address2Property);
			set => this.SetValue(Address2Property, value);
		}

		/// <summary>
		/// The <see cref="ZipCode"/>
		/// </summary>
		public static readonly BindableProperty ZipCodeProperty =
			BindableProperty.Create(nameof(ZipCode), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's zip code
		/// </summary>
		public string ZipCode
		{
			get => (string)this.GetValue(ZipCodeProperty);
			set => this.SetValue(ZipCodeProperty, value);
		}

		/// <summary>
		/// The <see cref="Area"/>
		/// </summary>
		public static readonly BindableProperty AreaProperty =
			BindableProperty.Create(nameof(Area), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's area
		/// </summary>
		public string Area
		{
			get => (string)this.GetValue(AreaProperty);
			set => this.SetValue(AreaProperty, value);
		}

		/// <summary>
		/// The <see cref="City"/>
		/// </summary>
		public static readonly BindableProperty CityProperty =
			BindableProperty.Create(nameof(City), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's city
		/// </summary>
		public string City
		{
			get => (string)this.GetValue(CityProperty);
			set => this.SetValue(CityProperty, value);
		}

		/// <summary>
		/// The <see cref="Region"/>
		/// </summary>
		public static readonly BindableProperty RegionProperty =
			BindableProperty.Create(nameof(Region), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's region
		/// </summary>
		public string Region
		{
			get => (string)this.GetValue(RegionProperty);
			set => this.SetValue(RegionProperty, value);
		}

		/// <summary>
		/// The <see cref="Country"/>
		/// </summary>
		public static readonly BindableProperty CountryProperty =
			BindableProperty.Create(nameof(Country), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// Gets or sets the user's country.
		/// </summary>
		public string Country
		{
			get => (string)this.GetValue(CountryProperty);
			set => this.SetValue(CountryProperty, value);
		}

		/// <summary>
		/// The <see cref="CountryCode"/>
		/// </summary>
		public static readonly BindableProperty CountryCodeProperty =
			BindableProperty.Create(nameof(CountryCode), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// Gets or sets the user's country.
		/// </summary>
		public string CountryCode
		{
			get => (string)this.GetValue(CountryCodeProperty);
			set => this.SetValue(CountryCodeProperty, value);
		}

		/// <summary>
		/// The <see cref="PhoneNr"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrProperty =
			BindableProperty.Create(nameof(PhoneNr), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// Gets or sets the user's country.
		/// </summary>
		public string PhoneNr
		{
			get => (string)this.GetValue(PhoneNrProperty);
			set => this.SetValue(PhoneNrProperty, value);
		}

		/// <summary>
		/// The <see cref="EMail"/>
		/// </summary>
		public static readonly BindableProperty EMailProperty =
			BindableProperty.Create(nameof(EMail), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// Gets or sets the user's country.
		/// </summary>
		public string EMail
		{
			get => (string)this.GetValue(EMailProperty);
			set => this.SetValue(EMailProperty, value);
		}

		/// <summary>
		/// The <see cref="IsApproved"/>
		/// </summary>
		public static readonly BindableProperty IsApprovedProperty =
			BindableProperty.Create(nameof(IsApproved), typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets if the user's identity is approved or not.
		/// </summary>
		public bool IsApproved
		{
			get => (bool)this.GetValue(IsApprovedProperty);
			set => this.SetValue(IsApprovedProperty, value);
		}

		/// <summary>
		/// The <see cref="IsCreated"/>
		/// </summary>
		public static readonly BindableProperty IsCreatedProperty =
			BindableProperty.Create(nameof(IsCreated), typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets if the user's identity has been created.
		/// </summary>
		public bool IsCreated
		{
			get => (bool)this.GetValue(IsCreatedProperty);
			set => this.SetValue(IsCreatedProperty, value);
		}

		/// <summary>
		/// The <see cref="IsConnected"/>
		/// </summary>
		public static readonly BindableProperty IsConnectedProperty =
			BindableProperty.Create(nameof(IsConnected), typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets if the app is connected to an XMPP server.
		/// </summary>
		public bool IsConnected
		{
			get => (bool)this.GetValue(IsConnectedProperty);
			set => this.SetValue(IsConnectedProperty, value);
		}

		/// <summary>
		/// The <see cref="ConnectionStateText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionStateTextProperty =
			BindableProperty.Create(nameof(ConnectionStateText), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user friendly connection state text to display to the user.
		/// </summary>
		public string ConnectionStateText
		{
			get => (string)this.GetValue(ConnectionStateTextProperty);
			set => this.SetValue(ConnectionStateTextProperty, value);
		}

		#endregion

		private void AssignProperties()
		{
			this.Created = this.TagProfile.LegalIdentity?.Created ?? DateTime.MinValue;
			this.Updated = this.TagProfile.LegalIdentity?.Updated.GetDateOrNullIfMinValue();
			this.LegalId = this.TagProfile.LegalIdentity?.Id;
			this.LegalIdentity = this.TagProfile.LegalIdentity;
			this.AssignBareJid();
			this.State = this.TagProfile.LegalIdentity?.State ?? IdentityState.Rejected;
			this.From = this.TagProfile.LegalIdentity?.From.GetDateOrNullIfMinValue();
			this.To = this.TagProfile.LegalIdentity?.To.GetDateOrNullIfMinValue();

			if (this.TagProfile.LegalIdentity is not null)
			{
				this.FirstName = this.TagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
				this.MiddleNames = this.TagProfile.LegalIdentity[Constants.XmppProperties.MiddleName];
				this.LastNames = this.TagProfile.LegalIdentity[Constants.XmppProperties.LastName];
				this.PersonalNumber = this.TagProfile.LegalIdentity[Constants.XmppProperties.PersonalNumber];
				this.Address = this.TagProfile.LegalIdentity[Constants.XmppProperties.Address];
				this.Address2 = this.TagProfile.LegalIdentity[Constants.XmppProperties.Address2];
				this.ZipCode = this.TagProfile.LegalIdentity[Constants.XmppProperties.ZipCode];
				this.Area = this.TagProfile.LegalIdentity[Constants.XmppProperties.Area];
				this.City = this.TagProfile.LegalIdentity[Constants.XmppProperties.City];
				this.Region = this.TagProfile.LegalIdentity[Constants.XmppProperties.Region];
				this.CountryCode = this.TagProfile.LegalIdentity[Constants.XmppProperties.Country];
				this.PhoneNr = this.TagProfile.LegalIdentity[Constants.XmppProperties.Phone];
				this.EMail = this.TagProfile.LegalIdentity[Constants.XmppProperties.EMail];
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
				this.EMail = string.Empty;
			}

			this.Country = ISO_3166_1.ToName(this.CountryCode);
			this.IsApproved = this.TagProfile.LegalIdentity?.State == IdentityState.Approved;
			this.IsCreated = this.TagProfile.LegalIdentity?.State == IdentityState.Created;

			this.ContinueCommand.ChangeCanExecute();
			this.RequestReviewCommand.ChangeCanExecute();

			this.SetConnectionStateAndText(this.XmppService.State);

			if (this.IsConnected)
				this.ReloadPhotos();
		}

		private void AssignBareJid()
		{
			this.BareJid = this.XmppService?.BareJid ?? string.Empty;
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.TagProfile.Step) || e.PropertyName == nameof(this.TagProfile.LegalIdentity))
				this.UiSerializer.BeginInvokeOnMainThread(this.AssignProperties);
			else
				this.UiSerializer.BeginInvokeOnMainThread(this.AssignBareJid);
		}

		private Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				this.AssignBareJid();
				this.SetConnectionStateAndText(NewState);
				this.RequestReviewCommand.ChangeCanExecute();
				if (this.IsConnected)
				{
					await Task.Delay(Constants.Timeouts.XmppInit);
					this.ReloadPhotos();
				}
			});

			return Task.CompletedTask;
		}

		private void SetConnectionStateAndText(XmppState state)
		{
			this.IsConnected = state == XmppState.Connected;
			this.ConnectionStateText = state.ToDisplayText();
		}

		private async void ReloadPhotos()
		{
			await this.reloadPhotosSemaphore.WaitAsync();

			try
			{
				this.photosLoader.CancelLoadPhotos();
				if (this.TagProfile?.LegalIdentity?.Attachments is not null)
				{
					// await is important, it prevents the semaphore from being released prematurely.
					_ = await this.photosLoader.LoadPhotos(this.TagProfile.LegalIdentity.Attachments, SignWith.LatestApprovedIdOrCurrentKeys);
				}
			}
			finally
			{
				this.reloadPhotosSemaphore.Release();
			}
		}

		private Task XmppContracts_LegalIdentityChanged(object Sender, LegalIdentityEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.LegalIdentity = e.Identity;
				this.TagProfile.SetLegalIdentity(e.Identity);
				this.AssignProperties();
			});

			return Task.CompletedTask;
		}

		private async Task RequestReview()
		{
			if (this.peerReviewServices is null)
			{
				if (!await this.NetworkService.TryRequest(async () =>
				{
					this.peerReviewServices = await this.XmppService.GetServiceProvidersForPeerReviewAsync();
				}))
				{
					return;
				}
			}

			if (this.peerReviewServices.Length > 0)
			{
				List<ServiceProviderWithLegalId> ServiceProviders = new();

				ServiceProviders.AddRange(this.peerReviewServices);
				ServiceProviders.Add(new RequestFromPeer());

				ServiceProvidersNavigationArgs e = new(ServiceProviders.ToArray(),
					LocalizationResourceManager.Current["SelectServiceProviderPeerReview"]);

				_ = App.Current.MainPage.Navigation.PushAsync(new ServiceProvidersPage(e));

				ServiceProviderWithLegalId ServiceProvider = (ServiceProviderWithLegalId)await e.WaitForServiceProviderSelection();
				if (ServiceProvider is not null)
				{
					await App.Current.MainPage.Navigation.PopAsync();

					if (string.IsNullOrEmpty(ServiceProvider.LegalId))
						await this.ScanQrCodeForPeerReview();
					else
					{
						if (!ServiceProvider.External)
						{
							if (!await this.NetworkService.TryRequest(async () => await this.XmppService.SelectPeerReviewService(ServiceProvider.Id, ServiceProvider.Type)))
								return;
						}

						await this.SendPeerReviewRequest(ServiceProvider.LegalId);
					}
				}
			}
			else
				await this.ScanQrCodeForPeerReview();
		}

		private async Task<bool> ScanQrCodeForPeerReview()
		{
			string Url = await QrCode.ScanQrCode(LocalizationResourceManager.Current["RequestReview"], UseShellNavigationService: false);
			if (string.IsNullOrEmpty(Url))
				return false;

			if (!Constants.UriSchemes.StartsWithIdScheme(Url))
			{
				if (!string.IsNullOrEmpty(Url))
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"],
						LocalizationResourceManager.Current["TheSpecifiedCodeIsNotALegalIdentity"]);
				}

				return false;
			}

			await this.SendPeerReviewRequest(Constants.UriSchemes.RemoveScheme(Url));

			return true;
		}

		private async Task SendPeerReviewRequest(string ToLegalId)
		{
			bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.PetitionPeerReviewId(
				ToLegalId, this.TagProfile.LegalIdentity, Guid.NewGuid().ToString(), LocalizationResourceManager.Current["CouldYouPleaseReviewMyIdentityInformation"]));

			if (succeeded)
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["PetitionSent"], LocalizationResourceManager.Current["APetitionHasBeenSentToYourPeer"]);
		}

		private void Continue()
		{
			this.TagProfile.SetIsValidated();
			this.OnStepCompleted(EventArgs.Empty);
		}
	}
}
