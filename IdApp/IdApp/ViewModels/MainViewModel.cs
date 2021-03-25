using IdApp.Extensions;
using IdApp.Services;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Views.Contracts;
using IdApp.Views.Wallet;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    /// <summary>
    /// The view model to bind to for the main page of the application. Holds basic user profile state
    /// as well as connection state.
    /// </summary>
    public class MainViewModel : NeuronViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly ILogService logService;
        private readonly INetworkService networkService;
        private readonly INavigationService navigationService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly IThingRegistryOrchestratorService thingRegistryOrchestratorService;
        private readonly IEDalerOrchestratorService eDalerOrchestratorService;
        private readonly PhotosLoader photosLoader;

        /// <summary>
        /// Creates a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
            : this(null, null, null, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MainViewModel"/> class.
        /// For unit tests.
        /// </summary>
        protected internal MainViewModel(
            ILogService logService,
            INeuronService neuronService, 
            IUiDispatcher uiDispatcher, 
            ITagProfile tagProfile,
            INavigationService navigationService,
            INetworkService networkService,
            IImageCacheService imageCacheService,
            IContractOrchestratorService contractOrchestratorService,
            IThingRegistryOrchestratorService thingThingRegistryOrchestratorService,
            IEDalerOrchestratorService eDalerOrchestratorService)
            : base(neuronService ?? DependencyService.Resolve<INeuronService>(), uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>())
        {
            this.logService = logService ?? DependencyService.Resolve<ILogService>();
            this.tagProfile = tagProfile ?? DependencyService.Resolve<ITagProfile>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            this.networkService = networkService ?? DependencyService.Resolve<INetworkService>();
            imageCacheService = imageCacheService ?? DependencyService.Resolve<IImageCacheService>();
            this.contractOrchestratorService = contractOrchestratorService ?? DependencyService.Resolve<IContractOrchestratorService>();
            this.thingRegistryOrchestratorService = thingThingRegistryOrchestratorService ?? DependencyService.Resolve<IThingRegistryOrchestratorService>();
            this.eDalerOrchestratorService = eDalerOrchestratorService ?? DependencyService.Resolve<IEDalerOrchestratorService>();
            this.photosLoader = new PhotosLoader(this.logService, this.networkService, this.NeuronService, this.UiDispatcher, imageCacheService);
            this.UpdateLoggedOutText(true);
            this.ViewMyContractsCommand = new Command(async () => await ViewMyContracts(), () => this.IsConnected);
            this.ScanQrCodeCommand = new Command(async () => await ScanQrCode());
            this.ViewWalletCommand = new Command(async () => await ViewWallet(), () => this.IsConnected);
        }

        /// <inheritdoc />
        protected override async Task DoBind()
        {
            await base.DoBind();
            this.AssignProperties();
            this.SetConnectionStateAndText(this.NeuronService.State);
            this.NeuronService.Contracts.ConnectionStateChanged += Contracts_ConnectionStateChanged;
            this.networkService.ConnectivityChanged += NetworkService_ConnectivityChanged;
        }

        /// <inheritdoc />
        protected override Task DoUnbind()
        {
            this.photosLoader.CancelLoadPhotos();
            this.NeuronService.Contracts.ConnectionStateChanged -= Contracts_ConnectionStateChanged;
            this.networkService.ConnectivityChanged -= NetworkService_ConnectivityChanged;
            return base.DoUnbind();
        }

        private void AssignProperties()
        {
            if (!(this.tagProfile?.LegalIdentity is null))
            {
                string firstName = this.tagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
                string lastNames = this.tagProfile.LegalIdentity[Constants.XmppProperties.LastName];

                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastNames))
                    this.FullName = $"{firstName} {lastNames}";
                else if (!string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastNames))
                    this.FullName = firstName;
                else if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastNames))
                    this.FullName = lastNames;
                else
                    this.FullName = string.Empty;

                this.City = this.tagProfile.LegalIdentity[Constants.XmppProperties.City];
                string countryCode = this.tagProfile.LegalIdentity[Constants.XmppProperties.Country];
                
                if (ISO_3166_1.TryGetCountry(countryCode, out string country))
                    this.Country = country;
                else
                    this.Country = string.Empty;
            }
            else
            {
                this.FullName = string.Empty;
                this.City = string.Empty;
                this.Country = string.Empty;
            }

            // QR
            if (!(this.tagProfile?.LegalIdentity is null))
            {
                _ = Task.Run(() =>
                {
                    byte[] bytes = QrCodeImageGenerator.GeneratePng(Constants.UriSchemes.CreateIdUri(this.tagProfile.LegalIdentity.Id), this.QrCodeWidth, this.QrCodeHeight);
                    if (this.IsBound)
                    {
                        this.UiDispatcher.BeginInvokeOnMainThread(() => this.QrCode = ImageSource.FromStream(() => new MemoryStream(bytes)));
                    }
                });
            }
            else
                this.QrCode = null;

            Attachment firstAttachment = this.tagProfile?.LegalIdentity?.Attachments?.GetFirstImageAttachment();
            if (!(firstAttachment is null))
            {
                // Don't await this one, just let it run asynchronously.
                _ = this.LoadProfilePhoto(firstAttachment);
            }
        }

        private async Task LoadProfilePhoto(Attachment firstAttachment)
        {
            try
            {
                bool connected = await this.NeuronService.WaitForConnectedState(Constants.Timeouts.XmppConnect);
                if (!connected)
                    return;

                MemoryStream ms = await this.photosLoader.LoadOnePhoto(firstAttachment, SignWith.LatestApprovedIdOrCurrentKeys);

                if (ms != null)
                {
                    byte[] bytes = ms.ToArray();
                    ms.Dispose();

                    this.UiDispatcher.BeginInvokeOnMainThread(() =>
                    {
                        if (this.IsBound)
                        {
                            Image = ImageSource.FromStream(() => new MemoryStream(bytes));
                        }
                    });
                }
            }
            catch (Exception e)
            {
                this.logService.LogException(e, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
            }
        }

        #region Properties

        /// <summary>
        /// See <see cref="ViewMyContractsCommand"/>
        /// </summary>
        public static readonly BindableProperty ViewMyContractsCommandProperty =
            BindableProperty.Create("ViewMyContractsCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

        /// <summary>
        /// The command to bind to for viewing the user's own contracts.
        /// </summary>
        public ICommand ViewMyContractsCommand
        {
            get { return (ICommand)GetValue(ViewMyContractsCommandProperty); }
            set { SetValue(ViewMyContractsCommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="ScanQrCodeCommand"/>
        /// </summary>
        public static readonly BindableProperty ScanQrCodeCommandProperty =
            BindableProperty.Create("ScanQrCodeCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

        /// <summary>
        /// The command to bind to for scanning a QR code.
        /// </summary>
        public ICommand ScanQrCodeCommand
        {
            get { return (ICommand)GetValue(ScanQrCodeCommandProperty); }
            set { SetValue(ScanQrCodeCommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="ViewWalletCommand"/>
        /// </summary>
        public static readonly BindableProperty ViewWalletCommandProperty =
            BindableProperty.Create("ViewWalletCommand", typeof(ICommand), typeof(MainViewModel), default(ICommand));

        /// <summary>
        /// The command to bind to for viewing a user's wallet.
        /// </summary>
        public ICommand ViewWalletCommand
        {
            get { return (ICommand)GetValue(ViewWalletCommandProperty); }
            set { SetValue(ViewWalletCommandProperty, value); }
        }

        /// <summary>
        /// See <see cref="HasPhoto"/>
        /// </summary>
        public static readonly BindableProperty HasPhotoProperty =
            BindableProperty.Create("HasPhoto", typeof(bool), typeof(MainViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the current user has a photo associated with the account.
        /// </summary>
        public bool HasPhoto
        {
            get { return (bool)GetValue(HasPhotoProperty); }
            set { SetValue(HasPhotoProperty, value); }
        }

        /// <summary>
        /// See <see cref="Image"/>
        /// </summary>
        public static readonly BindableProperty ImageProperty =
            BindableProperty.Create("Image", typeof(ImageSource), typeof(MainViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel viewModel = (MainViewModel)b;
                viewModel.HasPhoto = !(newValue is null);
            });

        /// <summary>
        /// Gets or sets the current user's photo.
        /// </summary>
        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        /// <summary>
        /// See <see cref="FullName"/>
        /// </summary>
        public static readonly BindableProperty FullNameProperty =
            BindableProperty.Create("FullName", typeof(string), typeof(MainViewModel), default(string));

        /// <summary>
        /// Gets or sets the current user's full name.
        /// </summary>
        public string FullName
        {
            get { return (string)GetValue(FullNameProperty); }
            set { SetValue(FullNameProperty, value); }
        }

        /// <summary>
        /// See <see cref="City"/>
        /// </summary>
        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel viewModel = (MainViewModel)b;
                viewModel.SetLocation();
            });

        /// <summary>
        /// Gets or sets the current user's city.
        /// </summary>
        public string City
        {
            get { return (string)GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        /// <summary>
        /// See <see cref="Country"/>
        /// </summary>
        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel viewModel = (MainViewModel)b;
                viewModel.SetLocation();
            });

        /// <summary>
        /// Gets or sets the current user's country.
        /// </summary>
        public string Country
        {
            get { return (string)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        /// <summary>
        /// See <see cref="Location"/>
        /// </summary>
        public static readonly BindableProperty LocationProperty =
            BindableProperty.Create("Location", typeof(string), typeof(MainViewModel), default(string));

        /// <summary>
        /// Gets or sets the current user's location.
        /// </summary>
        public string Location
        {
            get { return (string) GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        /// <summary>
        /// See <see cref="QrCode"/>
        /// </summary>
        public static readonly BindableProperty QrCodeProperty =
            BindableProperty.Create("QrCode", typeof(ImageSource), typeof(MainViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel viewModel = (MainViewModel)b;
                viewModel.HasQrCode = !(newValue is null);
            });


        /// <summary>
        /// Gets or sets the current user's identity as a QR code image.
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
            BindableProperty.Create("HasQrCode", typeof(bool), typeof(MainViewModel), default(bool));

        /// <summary>
        /// Gets or sets if a <see cref="QrCode"/> exists for the current user.
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
            BindableProperty.Create("QrCodeWidth", typeof(int), typeof(MainViewModel), UiConstants.QrCode.DefaultImageWidth);

        /// <summary>
        /// Gets or sets the width, in pixels, of the <see cref="QrCode"/> being generated.
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
            BindableProperty.Create("QrCodeHeight", typeof(int), typeof(MainViewModel), UiConstants.QrCode.DefaultImageHeight);

        /// <summary>
        /// Gets or sets the height, in pixels, of the <see cref="QrCode"/> being generated.
        /// </summary>
        public int QrCodeHeight
        {
            get { return (int)GetValue(QrCodeHeightProperty); }
            set { SetValue(QrCodeHeightProperty, value); }
        }

        /// <summary>
        /// See <see cref="IsOnline"/>
        /// </summary>
        public static readonly BindableProperty IsOnlineProperty =
            BindableProperty.Create("IsOnline", typeof(bool), typeof(MainViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the app is currently online, i.e. has network access.
        /// </summary>
        public bool IsOnline
        {
            get { return (bool)GetValue(IsOnlineProperty); }
            set { SetValue(IsOnlineProperty, value); }
        }

        /// <summary>
        /// See <see cref="NetworkStateText"/>
        /// </summary>
        public static readonly BindableProperty NetworkStateTextProperty =
            BindableProperty.Create("NetworkStateText", typeof(string), typeof(MainViewModel), default(string));

        /// <summary>
        /// Gets or sets the user friendly network state text for display.
        /// </summary>
        public string NetworkStateText
        {
            get { return (string)GetValue(NetworkStateTextProperty); }
            set { SetValue(NetworkStateTextProperty, value); }
        }

        /// <summary>
        /// See <see cref="ContractsIsOnline"/>
        /// </summary>
        public static readonly BindableProperty ContractsIsOnlineProperty =
            BindableProperty.Create("ContractsIsOnline", typeof(bool), typeof(MainViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the contracts features of the Neuron server is online.
        /// </summary>
        public bool ContractsIsOnline
        {
            get { return (bool)GetValue(ContractsIsOnlineProperty); }
            set { SetValue(ContractsIsOnlineProperty, value); }
        }

        /// <summary>
        /// See <see cref="ContractsStateText"/>
        /// </summary>
        public static readonly BindableProperty ContractsStateTextProperty =
            BindableProperty.Create("ContractsStateText", typeof(string), typeof(MainViewModel), default(string));

        /// <summary>
        /// Gets or sets the user friendly contracts connection state text for display.
        /// </summary>
        public string ContractsStateText
        {
            get { return (string)GetValue(ContractsStateTextProperty); }
            set { SetValue(ContractsStateTextProperty, value); }
        }

        /// <summary>
        /// See <see cref="HasConnectionErrors"/>
        /// </summary>
        public static readonly BindableProperty HasConnectionErrorsProperty =
            BindableProperty.Create("HasConnectionErrors", typeof(bool), typeof(MainViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether there are any connection errors at all for the app.
        /// </summary>
        public bool HasConnectionErrors
        {
            get { return (bool)GetValue(HasConnectionErrorsProperty); }
            set { SetValue(HasConnectionErrorsProperty, value); }
        }

        /// <summary>
        /// See <see cref="ConnectionErrorsText"/>
        /// </summary>
        public static readonly BindableProperty ConnectionErrorsTextProperty =
            BindableProperty.Create("ConnectionErrorsText", typeof(string), typeof(MainViewModel), default(string));

        /// <summary>
        /// Gets or sets the user friendly connection errors text for display. Can be null.
        /// </summary>
        public string ConnectionErrorsText
        {
            get { return (string)GetValue(ConnectionErrorsTextProperty); }
            set { SetValue(ConnectionErrorsTextProperty, value); }
        }

        /// <summary>
        /// See <see cref="YouAreNowLoggedOutText"/>
        /// </summary>
        public static readonly BindableProperty YouAreNowLoggedOutTextProperty =
            BindableProperty.Create("YouAreNowLoggedOutText", typeof(string), typeof(MainViewModel), default(string));

        /// <summary>
        /// The text to display in the UI, on the logout panel, depending on whether the user has manually logged out or in.
        /// </summary>
        public string YouAreNowLoggedOutText
        {
            get { return (string) GetValue(YouAreNowLoggedOutTextProperty); }
            set { SetValue(YouAreNowLoggedOutTextProperty, value); }
        }

        #endregion

        private async Task ViewMyContracts()
        {
            await this.navigationService.GoToAsync(nameof(MyContractsPage));
        }

        private async Task ViewWallet()
        {
            await this.eDalerOrchestratorService.OpenWallet();
        }

        private async Task ScanQrCode()
        {
            await IdApp.QrCode.ScanQrCodeAndHandleResult(this.logService, this.NeuronService, this.navigationService,
                this.UiDispatcher, this.contractOrchestratorService, this.thingRegistryOrchestratorService,
                this.eDalerOrchestratorService);
        }

        private void UpdateLoggedOutText(bool isLoggedOut)
        {
            this.YouAreNowLoggedOutText = isLoggedOut ? AppResources.YouHaveNowBeenSignedOut : AppResources.SigningIn;
        }

        private void SetLocation()
        {
            if (!string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(Country))
            {
                Location = $"{City}, {Country}";
            }
            if (!string.IsNullOrWhiteSpace(City) && string.IsNullOrWhiteSpace(Country))
            {
                Location = City;
            }
            if (string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(Country))
            {
                Location = Country;
            }
        }

        /// <inheritdoc />
        protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.SetConnectionStateAndText(e.State);
                if (e.IsUserInitiated)
                {
                    this.UpdateLoggedOutText(e.State == XmppState.Offline);
                }
            });
        }

        private void Contracts_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.SetConnectionStateAndText(this.NeuronService.State);
            });
        }

        private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => SetConnectionStateAndText(this.NeuronService.State));
        }

        /// <inheritdoc/>
        protected override void SetConnectionStateAndText(XmppState state)
        {
            // Network
            this.IsOnline = this.networkService.IsOnline;
            this.NetworkStateText = this.IsOnline ? AppResources.Online : AppResources.Offline;
            
            // Neuron server
            this.IsConnected = state == XmppState.Connected;
            this.ConnectionStateText = state.ToDisplayText(this.tagProfile);
            
            // Neuron Contracts
            this.ContractsIsOnline = this.NeuronService.IsOnline && this.NeuronService.Contracts.IsOnline;
            this.ContractsStateText = this.ContractsIsOnline ? AppResources.Online : AppResources.Offline;

            // Any connection errors or general errors that should be displayed?
            string latestError = this.NeuronService.LatestError;
            string latestConnectionError = this.NeuronService.LatestConnectionError;
            if (!string.IsNullOrWhiteSpace(latestError) && !string.IsNullOrWhiteSpace(latestConnectionError))
            {
                if (latestConnectionError != latestError)
                    this.ConnectionErrorsText = $"{latestConnectionError}{Environment.NewLine}{latestError}";
                else
                    this.ConnectionErrorsText = latestConnectionError;
            }
            else if (!string.IsNullOrWhiteSpace(latestConnectionError) && string.IsNullOrWhiteSpace(latestError))
            {
                this.ConnectionErrorsText = latestConnectionError;
            }
            else if (string.IsNullOrWhiteSpace(latestConnectionError) && !string.IsNullOrWhiteSpace(latestError))
            {
                this.ConnectionErrorsText = latestError;
            }
            else
            {
                this.ConnectionErrorsText = string.Empty;
            }
            this.HasConnectionErrors = !string.IsNullOrWhiteSpace(this.ConnectionErrorsText);
            this.EvaluateCommands(this.ViewMyContractsCommand, this.ScanQrCodeCommand, this.ViewWalletCommand);
        }
    }
}