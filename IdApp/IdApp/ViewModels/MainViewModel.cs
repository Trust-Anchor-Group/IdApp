using IdApp.Extensions;
using IdApp.Views.Registration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI;
using Tag.Neuron.Xamarin.UI.Extensions;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    public class MainViewModel : NeuronViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INetworkService networkService;
        private readonly ILogService logService;
        private readonly INavigationService navigationService;

        public MainViewModel()
            : this(DependencyService.Resolve<INeuronService>(), DependencyService.Resolve<IUiDispatcher>(), null, null, null, null)
        {
            this.tagProfile = DependencyService.Resolve<ITagProfile>();
            this.networkService = DependencyService.Resolve<INetworkService>();
            this.logService = DependencyService.Resolve<ILogService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.RevokeCommand = new Command(async _ => await Revoke(), _ => IsConnected);
            this.CompromiseCommand = new Command(async _ => await Compromise(), _ => IsConnected);
        }

        // For unit tests
        internal MainViewModel(INeuronService neuronService, IUiDispatcher uiDispatcher, ITagProfile tagProfile, INetworkService networkService, ILogService logService, INavigationService navigationService)
            : base(neuronService, uiDispatcher)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<ITagProfile>();
            this.networkService = networkService ?? DependencyService.Resolve<INetworkService>();
            this.logService = logService ?? DependencyService.Resolve<ILogService>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            this.RevokeCommand = new Command(async _ => await Revoke(), _ => IsConnected);
            this.CompromiseCommand = new Command(async _ => await Compromise(), _ => IsConnected);
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.IsOnline = this.networkService.IsOnline;
            this.ContractsIsOnline = this.NeuronService.Contracts.IsOnline;
            this.NeuronService.Contracts.ConnectionStateChanged += Contracts_ConnectionStateChanged;
            this.networkService.ConnectivityChanged += NetworkService_ConnectivityChanged;
            this.AssignProperties();
        }

        protected override Task DoUnbind()
        {
            this.NeuronService.Contracts.ConnectionStateChanged -= Contracts_ConnectionStateChanged;
            this.networkService.ConnectivityChanged -= NetworkService_ConnectivityChanged;
            return base.DoUnbind();
        }

        private void AssignProperties()
        {
            if (this.tagProfile?.LegalIdentity != null)
            {
                string firstName = this.tagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
                string lastNames = this.tagProfile.LegalIdentity[Constants.XmppProperties.LastName];

                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastNames))
                {
                    this.FullName = $"{firstName} {lastNames}";
                }
                else if (!string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastNames))
                {
                    this.FullName = firstName;
                }
                else if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastNames))
                {
                    this.FullName = lastNames;
                }
                else
                {
                    this.FullName = string.Empty;
                }

                this.City = this.tagProfile.LegalIdentity[Constants.XmppProperties.City];
                string countryCode = this.tagProfile.LegalIdentity[Constants.XmppProperties.Country];
                if (ISO_3166_1.TryGetCountry(countryCode, out string country))
                {
                    this.Country = country;
                }
                else
                {
                    this.Country = string.Empty;
                }
            }
            else
            {
                this.FullName = string.Empty;
                this.City = string.Empty;
                this.Country = string.Empty;
            }

            // QR
            if (this.tagProfile?.LegalIdentity != null)
            {
                _ = Task.Run(() =>
                {
                    byte[] png = QrCodeImageGenerator.GeneratePng(Constants.IoTSchemes.CreateIdUri(this.tagProfile.LegalIdentity.Id), this.QrCodeWidth, this.QrCodeHeight);
                    if (this.IsBound)
                    {
                        this.UiDispatcher.BeginInvokeOnMainThread(() => this.QrCode = ImageSource.FromStream(() => new MemoryStream(png)));
                    }
                });
            }
            else
            {
                this.QrCode = null;
            }
        }

        #region Properties

        public ICommand CompromiseCommand { get; }
        public ICommand RevokeCommand { get; }

        public static readonly BindableProperty HasPhotoProperty =
            BindableProperty.Create("HasPhoto", typeof(bool), typeof(MainViewModel), default(bool));

        public bool HasPhoto
        {
            get { return (bool)GetValue(HasPhotoProperty); }
            set { SetValue(HasPhotoProperty, value); }
        }

        public static readonly BindableProperty ImageProperty =
            BindableProperty.Create("Image", typeof(ImageSource), typeof(MainViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel viewModel = (MainViewModel)b;
                viewModel.HasPhoto = newValue != null;
            });

        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly BindableProperty FullNameProperty =
            BindableProperty.Create("FullName", typeof(string), typeof(MainViewModel), default(string));

        public string FullName
        {
            get { return (string)GetValue(FullNameProperty); }
            set { SetValue(FullNameProperty, value); }
        }

        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel viewModel = (MainViewModel)b;
                viewModel.SetLocation();
            });

        public string City
        {
            get { return (string)GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        public static readonly BindableProperty CountryProperty =
            BindableProperty.Create("Country", typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel viewModel = (MainViewModel)b;
                viewModel.SetLocation();
            });

        public string Country
        {
            get { return (string)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        public static readonly BindableProperty LocationProperty =
            BindableProperty.Create("Location", typeof(string), typeof(MainViewModel), default(string));

        public string Location
        {
            get { return (string) GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
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

        public static readonly BindableProperty QrCodeProperty =
            BindableProperty.Create("QrCode", typeof(ImageSource), typeof(MainViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel viewModel = (MainViewModel)b;
                viewModel.HasQrCode = newValue != null;
            });

        public ImageSource QrCode
        {
            get { return (ImageSource)GetValue(QrCodeProperty); }
            set { SetValue(QrCodeProperty, value); }
        }

        public static readonly BindableProperty HasQrCodeProperty =
            BindableProperty.Create("HasQrCode", typeof(bool), typeof(MainViewModel), default(bool));

        public bool HasQrCode
        {
            get { return (bool)GetValue(HasQrCodeProperty); }
            set { SetValue(HasQrCodeProperty, value); }
        }

        public static readonly BindableProperty QrCodeWidthProperty =
            BindableProperty.Create("QrCodeWidth", typeof(int), typeof(MainViewModel), UiConstants.QrCode.DefaultImageWidth);

        public int QrCodeWidth
        {
            get { return (int)GetValue(QrCodeWidthProperty); }
            set { SetValue(QrCodeWidthProperty, value); }
        }

        public static readonly BindableProperty QrCodeHeightProperty =
            BindableProperty.Create("QrCodeHeight", typeof(int), typeof(MainViewModel), UiConstants.QrCode.DefaultImageHeight);

        public int QrCodeHeight
        {
            get { return (int)GetValue(QrCodeHeightProperty); }
            set { SetValue(QrCodeHeightProperty, value); }
        }

        public static readonly BindableProperty IsOnlineProperty =
            BindableProperty.Create("IsOnline", typeof(bool), typeof(MainViewModel), default(bool), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel model = (MainViewModel)b;
                model.NetworkStateText = model.IsOnline ? AppResources.Online : AppResources.Offline;
            });

        public bool IsOnline
        {
            get { return (bool)GetValue(IsOnlineProperty); }
            set { SetValue(IsOnlineProperty, value); }
        }

        public static readonly BindableProperty NetworkStateTextProperty =
            BindableProperty.Create("NetworkStateText", typeof(string), typeof(MainViewModel), default(string));

        public string NetworkStateText
        {
            get { return (string)GetValue(NetworkStateTextProperty); }
            set { SetValue(NetworkStateTextProperty, value); }
        }

        public static readonly BindableProperty ContractsIsOnlineProperty =
            BindableProperty.Create("ContractsIsOnline", typeof(bool), typeof(MainViewModel), default(bool), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel model = (MainViewModel)b;
                model.ContractsStateText = model.ContractsIsOnline ? AppResources.Online : AppResources.Offline;
            });

        public bool ContractsIsOnline
        {
            get { return (bool)GetValue(ContractsIsOnlineProperty); }
            set { SetValue(ContractsIsOnlineProperty, value); }
        }

        public static readonly BindableProperty ContractsStateTextProperty =
            BindableProperty.Create("ContractsStateText", typeof(string), typeof(MainViewModel), default(string));

        public string ContractsStateText
        {
            get { return (string)GetValue(ContractsStateTextProperty); }
            set { SetValue(ContractsStateTextProperty, value); }
        }

        public static readonly BindableProperty HasConnectionErrorsProperty =
            BindableProperty.Create("HasConnectionErrors", typeof(bool), typeof(MainViewModel), default(bool));

        public bool HasConnectionErrors
        {
            get { return (bool)GetValue(HasConnectionErrorsProperty); }
            set { SetValue(HasConnectionErrorsProperty, value); }
        }

        public static readonly BindableProperty ConnectionErrorsTextProperty =
            BindableProperty.Create("ConnectionErrorsText", typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel model = (MainViewModel)b;
                model.HasConnectionErrors = !string.IsNullOrWhiteSpace((string)newValue);
            });

        public string ConnectionErrorsText
        {
            get { return (string)GetValue(ConnectionErrorsTextProperty); }
            set { SetValue(ConnectionErrorsTextProperty, value); }
        }
        #endregion

        private async Task Revoke()
        {
            try
            {
                if (!await this.UiDispatcher.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToRevokeYourLegalIdentity, AppResources.Yes, AppResources.No))
                    return;

                (bool succeeded, LegalIdentity revokedIdentity) = await this.networkService.TryRequest(this.NeuronService.Contracts.ObsoleteLegalIdentityAsync, this.tagProfile.LegalIdentity.Id);
                if (succeeded)
                {
                    this.tagProfile.RevokeLegalIdentity(revokedIdentity);
                    await this.navigationService.GoToAsync($"/{nameof(RegistrationPage)}");
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.UiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task Compromise()
        {
            try
            {
                if (!await this.UiDispatcher.DisplayAlert(AppResources.Confirm, AppResources.AreYouSureYouWantToReportYourLegalIdentityAsCompromized, AppResources.Yes, AppResources.No))
                    return;

                (bool succeeded, LegalIdentity compromisedIdentity) = await this.networkService.TryRequest(this.NeuronService.Contracts.CompromisedLegalIdentityAsync, this.tagProfile.LegalIdentity.Id);

                if (succeeded)
                {
                    this.tagProfile.RevokeLegalIdentity(compromisedIdentity);
                    await this.navigationService.GoToAsync($"/{nameof(RegistrationPage)}");
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.UiDispatcher.DisplayAlert(ex);
            }
        }

        protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.SetConnectionStateAndText(e.State);
                this.RevokeCommand.ChangeCanExecute();
                this.CompromiseCommand.ChangeCanExecute();
            });
            this.ContractsIsOnline = this.NeuronService.IsOnline && this.NeuronService.Contracts.IsOnline;
        }

        private void Contracts_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.ContractsIsOnline = this.NeuronService.IsOnline && this.NeuronService.Contracts.IsOnline;
                SetConnectionErrorText();
            });
        }

        private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => this.IsOnline = this.networkService.IsOnline);
        }

        private void SetConnectionErrorText()
        {
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
        }
        protected override void SetConnectionStateAndText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(this.tagProfile.Domain);
            this.IsConnected = state == XmppState.Connected;
            SetConnectionErrorText();
        }
    }
}