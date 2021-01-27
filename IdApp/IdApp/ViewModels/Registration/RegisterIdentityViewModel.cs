﻿using IdApp.Extensions;
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
    public class RegisterIdentityViewModel : RegistrationStepViewModel
    {
        private const string ProfilePhotoFileName = "ProfilePhoto.jpg";
        private readonly string localPhotoFileName;
        private LegalIdentityAttachment photo;
        private readonly INetworkService networkService;

        public RegisterIdentityViewModel(
            ITagProfile tagProfile,
            IUiDispatcher uiDispatcher,
            INeuronService neuronService, 
            INavigationService navigationService, 
            ISettingsService settingsService,
            INetworkService networkService,
            ILogService logService)
         : base(RegistrationStep.RegisterIdentity, tagProfile, uiDispatcher, neuronService, navigationService, settingsService, logService)
        {
            this.networkService = networkService;
            IDeviceInformation deviceInfo = DependencyService.Get<IDeviceInformation>();
            this.DeviceId = deviceInfo?.GetDeviceID();
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
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        protected override async Task DoUnbind()
        {
            this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        #region Properties

        public ICommand RegisterCommand { get; }
        public ICommand TakePhotoCommand { get; }
        public ICommand PickPhotoCommand { get; }
        public ICommand RemovePhotoCommand { get; }

        public static readonly BindableProperty HasPhotoProperty =
            BindableProperty.Create("HasPhoto", typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

        public bool HasPhoto
        {
            get { return (bool) GetValue(HasPhotoProperty); }
            set { SetValue(HasPhotoProperty, value); }
        }

        public static readonly BindableProperty ImageProperty =
            BindableProperty.Create("Image", typeof(ImageSource), typeof(RegisterIdentityViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
                viewModel.HasPhoto = newValue != null;
            });

        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public ObservableCollection<string> Countries { get; }

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

        public string SelectedCountry
        {
            get { return (string)GetValue(SelectedCountryProperty); }
            set { SetValue(SelectedCountryProperty, value); }
        }

        public static readonly BindableProperty FirstNameProperty =
            BindableProperty.Create("FirstName", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        public string FirstName
        {
            get { return (string)GetValue(FirstNameProperty); }
            set { SetValue(FirstNameProperty, value); }
        }

        public static readonly BindableProperty MiddleNamesProperty =
            BindableProperty.Create("MiddleNames", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        public string MiddleNames
        {
            get { return (string)GetValue(MiddleNamesProperty); }
            set { SetValue(MiddleNamesProperty, value); }
        }

        public static readonly BindableProperty LastNamesProperty =
            BindableProperty.Create("LastNames", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        public string LastNames
        {
            get { return (string)GetValue(LastNamesProperty); }
            set { SetValue(LastNamesProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberProperty =
            BindableProperty.Create("PersonalNumber", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        public string PersonalNumber
        {
            get { return (string)GetValue(PersonalNumberProperty); }
            set { SetValue(PersonalNumberProperty, value); }
        }

        public static readonly BindableProperty PersonalNumberPlaceholderProperty =
            BindableProperty.Create("PersonalNumberPlaceholder", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string PersonalNumberPlaceholder
        {
            get { return (string) GetValue(PersonalNumberPlaceholderProperty); }
            set { SetValue(PersonalNumberPlaceholderProperty, value); }
        }

        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create("Address", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly BindableProperty Address2Property =
            BindableProperty.Create("Address2", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string Address2
        {
            get { return (string)GetValue(Address2Property); }
            set { SetValue(Address2Property, value); }
        }

        public static readonly BindableProperty ZipCodeProperty =
            BindableProperty.Create("ZipCode", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        public string ZipCode
        {
            get { return (string)GetValue(ZipCodeProperty); }
            set { SetValue(ZipCodeProperty, value); }
        }

        public static readonly BindableProperty AreaProperty =
            BindableProperty.Create("Area", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string Area
        {
            get { return (string)GetValue(AreaProperty); }
            set { SetValue(AreaProperty, value); }
        }

        public static readonly BindableProperty CityProperty =
            BindableProperty.Create("City", typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

        public string City
        {
            get { return (string)GetValue(CityProperty); }
            set { SetValue(CityProperty, value); }
        }

        public static readonly BindableProperty RegionProperty =
            BindableProperty.Create("Region", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string Region
        {
            get { return (string)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        public static readonly BindableProperty DeviceIdProperty =
            BindableProperty.Create("DeviceId", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string DeviceId
        {
            get { return (string)GetValue(DeviceIdProperty); }
            set { SetValue(DeviceIdProperty, value); }
        }

        public LegalIdentity LegalIdentity { get; private set; }

        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

        public bool IsConnected
        {
            get { return (bool) GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(RegisterIdentityViewModel), default(string));

        public string ConnectionStateText
        {
            get { return (string) GetValue(ConnectionStateTextProperty); }
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

        private async Task AddPhoto(string filePath, bool saveLocalCopy)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                using (Stream f = File.OpenRead(filePath))
                {
                    f.CopyTo(ms);
                }
                ms.Reset();
            }
            catch (Exception e)
            {
                await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToLoadPhoto);
                this.LogService.LogException(e);
                return;
            }

            byte[] bytes = ms.ToArray();

            if (bytes.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
            {
                ms.Dispose();
                await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PhotoIsTooLarge);
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
            this.photo = new LegalIdentityAttachment(filePath, Constants.MimeTypes.Jpeg, bytes);
            ms.Reset();
            Image = ImageSource.FromStream(() => ms); // .FromStream disposes the stream
            RegisterCommand.ChangeCanExecute();
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
                LegalIdentityAttachment[] photos = { photo };
                (bool succeeded, LegalIdentity addedIdentity) = await this.networkService.TryRequest(this.NeuronService.Contracts.AddLegalIdentity, CreateRegisterModel(), photos);
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

            if (!string.IsNullOrWhiteSpace(s = this.DeviceId?.Trim()))
                model.DeviceId = s;

            return model;
        }

        private async Task<bool> ValidateInput(bool alertUser)
        {
            if (string.IsNullOrWhiteSpace(this.FirstName?.Trim()))
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideAFirstName);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.LastNames?.Trim()))
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideALastName);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.PersonalNumber?.Trim()))
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideAPersonalNumber);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.SelectedCountry))
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideACountry);
                }
                return false;
            }

            if (this.photo == null)
            {
                if (alertUser)
                {
                    await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.YouNeedToProvideAPhoto);
                }

                return false;
            }

            return true;
        }

        protected override async Task DoSaveState()
        {
            await base.DoSaveState();
            this.SettingsService.SaveState(GetSettingsKey(nameof(SelectedCountry)), this.SelectedCountry);
            this.SettingsService.SaveState(GetSettingsKey(nameof(FirstName)), this.FirstName);
            this.SettingsService.SaveState(GetSettingsKey(nameof(MiddleNames)), this.MiddleNames);
            this.SettingsService.SaveState(GetSettingsKey(nameof(LastNames)), this.LastNames);
            this.SettingsService.SaveState(GetSettingsKey(nameof(PersonalNumber)), this.PersonalNumber);
            this.SettingsService.SaveState(GetSettingsKey(nameof(Address)), this.Address);
            this.SettingsService.SaveState(GetSettingsKey(nameof(Address2)), this.Address2);
            this.SettingsService.SaveState(GetSettingsKey(nameof(Area)), this.Area);
            this.SettingsService.SaveState(GetSettingsKey(nameof(City)), this.City);
            this.SettingsService.SaveState(GetSettingsKey(nameof(ZipCode)), this.ZipCode);
            this.SettingsService.SaveState(GetSettingsKey(nameof(Region)), this.Region);
        }

        protected override async Task DoRestoreState()
        {
            this.SelectedCountry = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(SelectedCountry)));
            this.FirstName = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(FirstName)));
            this.MiddleNames  = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(MiddleNames)));
            this.LastNames = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(LastNames)));
            this.PersonalNumber = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(PersonalNumber)));
            this.Address = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(Address)));
            this.Address2 = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(Address2)));
            this.Area = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(Area)));
            this.City = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(City)));
            this.ZipCode = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(ZipCode)));
            this.Region = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(Region)));
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
    }
}