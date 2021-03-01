using IdApp.Extensions;
using IdApp.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Models;
using Tag.Neuron.Xamarin.PersonalNumbers;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Extensions;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.ViewModels.Registration
{
    /// <summary>
    /// The view model to bind to when showing Step 3 of the registration flow: registering an identity.
    /// </summary>
    public class RegisterIdentityViewModel : RegistrationStepViewModel
    {
        private const string ProfilePhotoFileName = "ProfilePhoto.jpg";
        private readonly string localPhotoFileName;
        private LegalIdentityAttachment photo;
        private readonly INetworkService networkService;
        private readonly PhotosLoader photosLoader;

        /// <summary>
        /// Creates a new instance of the <see cref="RegisterIdentityModel"/> class.
        /// <param name="tagProfile">The tag profile to work with.</param>
        /// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
        /// <param name="neuronService">The Neuron service for XMPP communication.</param>
        /// <param name="navigationService">The navigation service to use for app navigation</param>
        /// <param name="settingsService">The settings service for persisting UI state.</param>
        /// <param name="networkService">The network service for network access.</param>
        /// <param name="logService">The log service.</param>
        /// <param name="imageCacheService">The image cache to use.</param>
        /// </summary>
        public RegisterIdentityViewModel(
            ITagProfile tagProfile,
            IUiDispatcher uiDispatcher,
            INeuronService neuronService,
            INavigationService navigationService,
            ISettingsService settingsService,
            INetworkService networkService,
            ILogService logService,
            IImageCacheService imageCacheService)
         : base(RegistrationStep.RegisterIdentity, tagProfile, uiDispatcher, neuronService, navigationService, settingsService, logService)
        {
            this.networkService = networkService;
            IDeviceInformation deviceInfo = DependencyService.Get<IDeviceInformation>();
            this.DeviceId = deviceInfo?.GetDeviceId();
            this.Countries = new ObservableCollection<string>();
            foreach (string country in ISO_3166_1.Countries)
                this.Countries.Add(country);
            this.SelectedCountry = null;
            this.RegisterCommand = new Command(async _ => await Register(), _ => CanRegister());
            this.TakePhotoCommand = new Command(async _ => await TakePhoto(), _ => !IsBusy);
            this.PickPhotoCommand = new Command(async _ => await PickPhoto(), _ => !IsBusy);
            this.RemovePhotoCommand = new Command(_ => RemovePhoto(true));
            this.Title = AppResources.PersonalLegalInformation;
            this.PersonalNumberPlaceholder = AppResources.PersonalNumber;
            this.localPhotoFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProfilePhotoFileName);
            imageCacheService = imageCacheService ?? DependencyService.Resolve<IImageCacheService>();
            this.photosLoader = new PhotosLoader(logService, networkService, neuronService, uiDispatcher, imageCacheService);
        }

        /// <inheritdoc />
        protected override async Task DoBind()
        {
            await base.DoBind();
            RegisterCommand.ChangeCanExecute();
            this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        /// <inheritdoc />
        protected override async Task DoUnbind()
        {
            this.photosLoader.CancelLoadPhotos();
            this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        #region Properties

        /// <summary>
        /// The command to bind to for performing the 'register' action.
        /// </summary>
        public ICommand RegisterCommand { get; }
        /// <summary>
        /// The command to bind to for taking a photo with the camera.
        /// </summary>
        public ICommand TakePhotoCommand { get; }
        /// <summary>
        /// The command to bind to for selecting a photo from the camera roll.
        /// </summary>
        public ICommand PickPhotoCommand { get; }
        /// <summary>
        /// The command to bind to for removing the currently selected photo.
        /// </summary>
        public ICommand RemovePhotoCommand { get; }

        /// <summary>
        /// The <see cref="HasPhoto"/>
        /// </summary>
        public static readonly BindableProperty HasPhotoProperty =
            BindableProperty.Create("HasPhoto", typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the user has selected a photo for their account or not.
        /// </summary>
        public bool HasPhoto
        {
            get { return (bool)GetValue(HasPhotoProperty); }
            set { SetValue(HasPhotoProperty, value); }
        }

        /// <summary>
        /// The <see cref="HasPhoto"/>
        /// </summary>
        public static readonly BindableProperty ImageProperty =
            BindableProperty.Create("Image", typeof(ImageSource), typeof(RegisterIdentityViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
                viewModel.HasPhoto = newValue != null;
            });

        /// <summary>
        /// The image source, i.e. the file representing the selected photo.
        /// </summary>
        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        /// <summary>
        /// The list of all available countries a user can select from.
        /// </summary>
        public ObservableCollection<string> Countries { get; }

        /// <summary>
        /// The <see cref="HasPhoto"/>
        /// </summary>
        public static readonly BindableProperty SelectedCountryProperty =
            BindableProperty.Create("SelectedCountry", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
                viewModel.RegisterCommand.ChangeCanExecute();
                if (!string.IsNullOrWhiteSpace(viewModel.SelectedCountry) && ISO_3166_1.TryGetCode(viewModel.SelectedCountry, out string code))
                {
                    string format = PersonalNumberSchemes.DisplayStringForCountry(code);
                    if (!string.IsNullOrWhiteSpace(format))
                    {
                        viewModel.PersonalNumberPlaceholder = string.Format(AppResources.PersonalNumberPlaceholder, format);
                    }
                    else
                    {
                        viewModel.PersonalNumberPlaceholder = AppResources.PersonalNumber;
                    }
                }
                else
                {
                    viewModel.PersonalNumberPlaceholder = AppResources.PersonalNumber;
                }
            });

        /// <summary>
        /// The user selected country from the list of <see cref="Countries"/>.
        /// </summary>
        public string SelectedCountry
        {
            get { return (string)GetValue(SelectedCountryProperty); }
            set { SetValue(SelectedCountryProperty, value); }
        }

        /// <summary>
        /// The <see cref="FirstName"/>
        /// </summary>
        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        /// <summary>
        /// The user's first name
        /// </summary>
        public string FirstName
        {
            get { return (string)GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        /// <summary>
        /// The <see cref="MiddleNames"/>
        /// </summary>
        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        /// <summary>
        /// The user's middle name(s)
        /// </summary>
        public string MiddleNames
        {
            get { return (string)GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        /// <summary>
        /// The <see cref="LastNames"/>
        /// </summary>
        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        /// <summary>
        /// The user's last name(s)
        /// </summary>
        public string LastNames
        {
            get { return (string)GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        /// <summary>
        /// The <see cref="PersonalNumber"/>
        /// </summary>
        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        /// <summary>
        /// The user's personal number
        /// </summary>
        public string PersonalNumber
        {
            get { return (string)GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        /// <summary>
        /// The <see cref="PersonalNumberPlaceholder"/>
        /// </summary>
        public static readonly BindableProperty PersonalNumberPlaceholderProperty =
            BindableProperty.Create("PersonalNumberPlaceholder", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        /// <summary>
        /// The personal number placeholder, used as a guide to the user to enter the correct format, which depends on the <see cref="SelectedCountry"/>.
        /// </summary>
        public string PersonalNumberPlaceholder
        {
            get { return (string)GetValue(PersonalNumberPlaceholderProperty); }
            set { SetValue(PersonalNumberPlaceholderProperty, value); }
        }

        /// <summary>
        /// The <see cref="Address"/>
        /// </summary>
        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        /// <summary>
        /// The user's address, line 1.
        /// </summary>
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        /// <summary>
        /// The <see cref="Address2"/>
        /// </summary>
        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        /// <summary>
        /// The user's address, line 2.
        /// </summary>
        public string Address2
        {
            get { return (string)GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        /// <summary>
        /// The <see cref="ZipCode"/>
        /// </summary>
        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        /// <summary>
        /// The user's zip code
        /// </summary>
        public string ZipCode
        {
            get { return (string)GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        /// <summary>
        /// The <see cref="Area"/>
        /// </summary>
        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        /// <summary>
        /// The user's area
        /// </summary>
        public string Area
        {
            get { return (string)GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        /// <summary>
        /// The <see cref="City"/>
        /// </summary>
        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        /// <summary>
        /// The user's city
        /// </summary>
        public string City
        {
            get { return (string)GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        /// <summary>
        /// The <see cref="Region"/>
        /// </summary>
        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        /// <summary>
        /// The user's region
        /// </summary>
        public string Region
        {
            get { return (string)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        /// <summary>
        /// The <see cref="DeviceId"/>
        /// </summary>
        public static readonly BindableProperty DeviceIdProperty =
            BindableProperty.Create("DeviceId", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        /// <summary>
        /// The device id.
        /// </summary>
        public string DeviceId
        {
            get { return (string)GetValue(DeviceIdProperty); }
            set { SetValue(DeviceIdProperty, value); }
        }

        /// <summary>
        /// The user's legal identity, set when the registration has occurred.
        /// </summary>
        public LegalIdentity LegalIdentity { get; private set; }

        /// <summary>
        /// The <see cref="HasPhoto"/>
        /// </summary>
        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the app is connected to an XMPP server.
        /// </summary>
        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        /// <summary>
        /// The <see cref="ConnectionStateText"/>
        /// </summary>
        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        /// <summary>
        /// The user friendly connection state text to display to the user.
        /// </summary>
        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        #endregion

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            UiDispatcher.BeginInvokeOnMainThread(() =>
            {
                SetConnectionStateAndText(e.State);
                RegisterCommand.ChangeCanExecute();
            });
        }

        private static void OnPropertyChanged(BindableObject b, object oldValue, object newValue)
        {
            RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
            viewModel.RegisterCommand.ChangeCanExecute();
        }

        private void SetConnectionStateAndText(XmppState state)
        {
            IsConnected = state == XmppState.Connected;
            this.ConnectionStateText = state.ToDisplayText(null);
        }

        private async Task TakePhoto()
        {
            if (!this.NeuronService.Contracts.FileUploadIsSupported)
            {
                await this.UiDispatcher.DisplayAlert(AppResources.TakePhoto, AppResources.TakingAPhotoIsNotSupported);
                return;
            }

            FileResult capturedPhoto = await MediaPicker.CapturePhotoAsync();
            if (capturedPhoto != null)
            {
                await AddPhoto(capturedPhoto.FullPath, true);
            }
        }

        private async Task PickPhoto()
        {
            if (!this.NeuronService.Contracts.FileUploadIsSupported)
            {
                await this.UiDispatcher.DisplayAlert(AppResources.PickPhoto, AppResources.SelectingAPhotoIsNotSupported);
                return;
            }

            FileResult pickedPhoto = await MediaPicker.PickPhotoAsync();
            if (pickedPhoto != null)
            {
                await AddPhoto(pickedPhoto.FullPath, true);
            }
        }

        /// <summary>
        /// Adds a photo from the specified path to use as a profile photo.
        /// </summary>
        /// <param name="ms">The file stream.</param>
        /// <param name="saveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
        /// <param name="showAlert">Set to <c>true</c> to show an alert if photo is too large; <c>false</c> otherwise.</param>
        /// <returns></returns>
        protected internal async Task AddPhoto(MemoryStream ms, bool saveLocalCopy, bool showAlert)
        {
            byte[] bytes = ms.ToArray();
            if (bytes.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
            {
                ms.Dispose();
                if (showAlert)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PhotoIsTooLarge);
                }
                return;
            }

            RemovePhoto(saveLocalCopy);

            if (saveLocalCopy)
            {
                try
                {
                    File.WriteAllBytes(localPhotoFileName, bytes);
                }
                catch (Exception e)
                {
                    this.LogService.LogException(e);
                }
            }
            this.photo = new LegalIdentityAttachment(localPhotoFileName, Constants.MimeTypes.Jpeg, bytes);
            ms.Reset();
            Image = ImageSource.FromStream(() => ms); // .FromStream disposes the stream
            RegisterCommand.ChangeCanExecute();
        }

        /// <summary>
        /// Adds a photo from the specified path to use as a profile photo.
        /// </summary>
        /// <param name="filePath">The full path to the file.</param>
        /// <param name="saveLocalCopy">Set to <c>true</c> to save a local copy, <c>false</c> otherwise.</param>
        /// <returns></returns>
        protected internal async Task AddPhoto(string filePath, bool saveLocalCopy)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                using (Stream f = File.OpenRead(filePath))
                {
                    await f.CopyToAsync(ms);
                }
                ms.Reset();
            }
            catch (Exception e)
            {
                await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToLoadPhoto);
                this.LogService.LogException(e);
                return;
            }

            await AddPhoto(ms, saveLocalCopy, true);
        }

        private void RemovePhoto(bool removeFileOnDisc)
        {
            try
            {
                this.photo = null;
                Image = null;
                if (removeFileOnDisc && File.Exists(this.localPhotoFileName))
                {
                    File.Delete(this.localPhotoFileName);
                }
            }
            catch (Exception e)
            {
                this.LogService.LogException(e);
            }
        }

        private async Task Register()
        {
            if (!(await this.ValidateInput(true)))
            {
                return;
            }

            string countryCode = ISO_3166_1.ToCode(this.SelectedCountry);
            string pnr = PersonalNumber.Trim();
            string pnrBeforeValidation = pnr;
            bool? personalNumberIsValid = PersonalNumberSchemes.IsValid(countryCode, ref pnr, out string personalNumberFormat);

            if (personalNumberIsValid.HasValue && !personalNumberIsValid.Value)
            {
                if (string.IsNullOrWhiteSpace(personalNumberFormat))
                    await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PersonalNumberDoesNotMatchCountry);
                else
                    await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PersonalNumberDoesNotMatchCountry_ExpectedFormat + personalNumberFormat);
                return;
            }
            if (pnr != pnrBeforeValidation)
            {
                this.PersonalNumber = pnr;
            }

            if (string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
            {
                await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.OperatorDoesNotSupportLegalIdentitiesAndSmartContracts);
                return;
            }

            if (!this.NeuronService.IsOnline)
            {
                await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.NotConnectedToTheOperator);
                return;
            }

            SetIsBusy(RegisterCommand, TakePhotoCommand, PickPhotoCommand);

            try
            {
                RegisterIdentityModel model = CreateRegisterModel();
                LegalIdentityAttachment[] photos = { photo };
                (bool succeeded, LegalIdentity addedIdentity) = await this.networkService.TryRequest(() => this.NeuronService.Contracts.AddLegalIdentity(model, photos));
                if (succeeded)
                {
                    this.LegalIdentity = addedIdentity;
                    this.TagProfile.SetLegalIdentity(this.LegalIdentity);
                    UiDispatcher.BeginInvokeOnMainThread(() =>
                    {
                        SetIsDone(RegisterCommand, TakePhotoCommand, PickPhotoCommand);
                        OnStepCompleted(EventArgs.Empty);
                    });
                }
            }
            catch (Exception ex)
            {
                this.LogService.LogException(ex);
                await this.UiDispatcher.DisplayAlert(ex);
            }
            finally
            {
                BeginInvokeSetIsDone(RegisterCommand, TakePhotoCommand, PickPhotoCommand);
            }
        }

        private bool CanRegister()
        {
            // Ok to 'wait' on, since we're not actually waiting on anything.
            return ValidateInput(false).GetAwaiter().GetResult() && this.NeuronService.IsOnline;
        }

        private RegisterIdentityModel CreateRegisterModel()
        {
            string s;
            RegisterIdentityModel model = new RegisterIdentityModel();
            if (!string.IsNullOrWhiteSpace(s = this.FirstName?.Trim()))
                model.FirstName = s;

            if (!string.IsNullOrWhiteSpace(s = this.MiddleNames?.Trim()))
                model.MiddleNames = s;

            if (!string.IsNullOrWhiteSpace(s = this.LastNames?.Trim()))
                model.LastNames = s;

            if (!string.IsNullOrWhiteSpace(s = this.PersonalNumber?.Trim()))
                model.PersonalNumber = s;

            if (!string.IsNullOrWhiteSpace(s = this.Address?.Trim()))
                model.Address = s;

            if (!string.IsNullOrWhiteSpace(s = this.Address2?.Trim()))
                model.Address2 = s;

            if (!string.IsNullOrWhiteSpace(s = this.ZipCode?.Trim()))
                model.ZipCode = s;

            if (!string.IsNullOrWhiteSpace(s = this.Area?.Trim()))
                model.Area = s;

            if (!string.IsNullOrWhiteSpace(s = this.City?.Trim()))
                model.City = s;

            if (!string.IsNullOrWhiteSpace(s = this.Region?.Trim()))
                model.Region = s;

            if (!string.IsNullOrWhiteSpace(s = this.SelectedCountry?.Trim()))
                model.Country = s;

            return model;
        }

        private async Task<bool> ValidateInput(bool alertUser)
        {
            if (string.IsNullOrWhiteSpace(this.FirstName?.Trim()))
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideAFirstName);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.LastNames?.Trim()))
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideALastName);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.PersonalNumber?.Trim()))
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideAPersonalNumber);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.SelectedCountry))
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideACountry);
                }
                return false;
            }

            if (this.photo == null && alertUser)
            {
                await this.UiDispatcher.DisplayAlert(AppResources.InformationIsMissingOrInvalid, AppResources.YouNeedToProvideAPhoto);

                return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override async Task DoSaveState()
        {
            await base.DoSaveState();
            await this.SettingsService.SaveState(GetSettingsKey(nameof(SelectedCountry)), this.SelectedCountry);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(FirstName)), this.FirstName);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(MiddleNames)), this.MiddleNames);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(LastNames)), this.LastNames);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(PersonalNumber)), this.PersonalNumber);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(Address)), this.Address);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(Address2)), this.Address2);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(Area)), this.Area);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(City)), this.City);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(ZipCode)), this.ZipCode);
            await this.SettingsService.SaveState(GetSettingsKey(nameof(Region)), this.Region);
        }

        /// <inheritdoc />
        protected override async Task DoRestoreState()
        {
            this.SelectedCountry = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(SelectedCountry)));
            this.FirstName = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(FirstName)));
            this.MiddleNames = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(MiddleNames)));
            this.LastNames = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(LastNames)));
            this.PersonalNumber = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(PersonalNumber)));
            this.Address = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(Address)));
            this.Address2 = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(Address2)));
            this.Area = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(Area)));
            this.City = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(City)));
            this.ZipCode = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(ZipCode)));
            this.Region = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(Region)));
            try
            {
                if (this.TagProfile.Step > RegistrationStep.Account && File.Exists(this.localPhotoFileName))
                {
                    await this.AddPhoto(this.localPhotoFileName, false);
                }
            }
            catch (Exception e)
            {
                this.LogService.LogException(e);
            }
            await base.DoRestoreState();
        }

        /// <inheritdoc />
        public override void ClearStepState()
        {
            RemovePhoto(true);
            this.SelectedCountry = null;
            this.FirstName = string.Empty;
            this.MiddleNames = string.Empty;
            this.LastNames = string.Empty;
            this.PersonalNumber = string.Empty;
            this.Address = string.Empty;
            this.Address2 = string.Empty;
            this.Area = string.Empty;
            this.City = string.Empty;
            this.ZipCode = string.Empty;
            this.Region = string.Empty;
            this.SettingsService.RemoveState(GetSettingsKey(nameof(SelectedCountry)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(FirstName)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(MiddleNames)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(LastNames)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(PersonalNumber)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(Address)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(Address2)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(Area)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(City)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(ZipCode)));
            this.SettingsService.RemoveState(GetSettingsKey(nameof(Region)));
        }

        /// <summary>
        /// Copies values from the existing TAG Profile's Legal identity.
        /// </summary>
        public virtual void PopulateFromTagProfile()
        {
            LegalIdentity identity = this.TagProfile.LegalIdentity;
            if (identity != null)
            {
                this.FirstName = identity[Constants.XmppProperties.FirstName];
                this.MiddleNames = identity[Constants.XmppProperties.MiddleName];
                this.LastNames = identity[Constants.XmppProperties.LastName];
                this.PersonalNumber = identity[Constants.XmppProperties.PersonalNumber];
                this.Address = identity[Constants.XmppProperties.Address];
                this.Address2 = identity[Constants.XmppProperties.Address2];
                this.ZipCode = identity[Constants.XmppProperties.ZipCode];
                this.Area = identity[Constants.XmppProperties.Area];
                this.City = identity[Constants.XmppProperties.City];
                this.Region = identity[Constants.XmppProperties.Region];
                string countryCode = identity[Constants.XmppProperties.Country];
                if (!string.IsNullOrWhiteSpace(countryCode) && ISO_3166_1.TryGetCountry(countryCode, out string country))
                {
                    this.SelectedCountry = country;
                }

                Attachment firstAttachment = identity.Attachments?.GetFirstImageAttachment();
                if (firstAttachment != null)
                {
                    // Don't await this one, just let it run asynchronously.
                    this.photosLoader
                        .LoadOnePhoto(firstAttachment, SignWith.LatestApprovedIdOrCurrentKeys)
                        .ContinueWith(task =>
                        {
                            MemoryStream stream = task.Result;
                            if (stream != null)
                            {
                                if (!this.IsBound) // Page no longer on screen when download is done?
                                {
                                    stream.Dispose();
                                    return;
                                }
                                this.UiDispatcher.BeginInvokeOnMainThread(async () =>
                                {
                                    await this.AddPhoto(stream, true, false);
                                });
                            }
                        }, TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnCanceled);
                }
            }
        }
    }
}