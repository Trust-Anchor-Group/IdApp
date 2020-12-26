using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class ChooseAccountViewModel : RegistrationStepViewModel
    {
        private readonly IAuthService authService;
        private readonly INetworkService networkService;

        public ChooseAccountViewModel(
            TagProfile tagProfile,
            INeuronService neuronService,
            INavigationService navigationService,
            ISettingsService settingsService,
            IAuthService authService,
            INetworkService networkService,
            ILogService logService)
            : base(RegistrationStep.Account, tagProfile, neuronService, navigationService, settingsService, logService)
        {
            this.authService = authService;
            this.networkService = networkService;
            this.PerformActionCommand = new Command(async _ => await PerformAction(), _ => CanPerformAction());
            this.SwitchModeCommand = new Command(_ =>
            {
                CreateNew = !CreateNew;
                Mode = CreateNew ? AccountMode.Create : AccountMode.Connect;
                PerformActionCommand.ChangeCanExecute();
            });
            this.ActionButtonText = AppResources.CreateNew;
            this.CreateNew = true;
            this.Mode = AccountMode.Create;
            this.CreateRandomPassword = true;
            this.Title = AppResources.ChooseAccount;
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.TagProfile.Changed += TagProfile_Changed;
        }

        protected override async Task DoUnbind()
        {
            this.TagProfile.Changed -= TagProfile_Changed;
            await base.DoUnbind();
        }

        private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvokeOnMainThread(() =>
            {
                IntroText = string.Format(AppResources.ToConnectToDomainYouNeedAnAccount, this.TagProfile.Domain);
            });
        }

        public AccountMode Mode { get; private set; }

        public static readonly BindableProperty IntroTextProperty =
            BindableProperty.Create("IntroText", typeof(string), typeof(ChooseAccountViewModel), default(string));

        public string IntroText
        {
            get { return (string)GetValue(IntroTextProperty); }
            set { SetValue(IntroTextProperty, value); }
        }

        public static readonly BindableProperty CreateNewProperty =
            BindableProperty.Create("CreateNew", typeof(bool), typeof(ChooseAccountViewModel), default(bool), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseAccountViewModel viewModel = (ChooseAccountViewModel)b;
                viewModel.ActionButtonText = (bool)newValue ? AppResources.CreateNew : AppResources.UseExisting;
                viewModel.UpdatePasswordState();
            });

        public bool CreateNew
        {
            get { return (bool)GetValue(CreateNewProperty); }
            set { SetValue(CreateNewProperty, value); }
        }

        public static readonly BindableProperty CreateRandomPasswordProperty =
            BindableProperty.Create("CreateRandomPassword", typeof(bool), typeof(ChooseAccountViewModel), default(bool), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseAccountViewModel viewModel = (ChooseAccountViewModel)b;
                viewModel.PerformActionCommand.ChangeCanExecute();
                viewModel.UpdatePasswordState();
            });

        public bool CreateRandomPassword
        {
            get { return (bool)GetValue(CreateRandomPasswordProperty); }
            set { SetValue(CreateRandomPasswordProperty, value); }
        }

        public static readonly BindableProperty CreateNewAccountNameProperty =
            BindableProperty.Create("CreateNewAccountName", typeof(string), typeof(ChooseAccountViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseAccountViewModel viewModel = (ChooseAccountViewModel)b;
                viewModel.PerformActionCommand.ChangeCanExecute();
            });

        public string CreateNewAccountName
        {
            get { return (string)GetValue(CreateNewAccountNameProperty); }
            set { SetValue(CreateNewAccountNameProperty, value); }
        }

        public static readonly BindableProperty ConnectToExistingAccountNameProperty =
            BindableProperty.Create("ConnectToExistingAccountName", typeof(string), typeof(ChooseAccountViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseAccountViewModel viewModel = (ChooseAccountViewModel)b;
                viewModel.PerformActionCommand.ChangeCanExecute();
            });

        public string ConnectToExistingAccountName
        {
            get { return (string)GetValue(ConnectToExistingAccountNameProperty); }
            set { SetValue(ConnectToExistingAccountNameProperty, value); }
        }

        public static readonly BindableProperty PasswordProperty =
            BindableProperty.Create("Password", typeof(string), typeof(ChooseAccountViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseAccountViewModel viewModel = (ChooseAccountViewModel)b;
                viewModel.UpdatePasswordState();
                viewModel.PerformActionCommand.ChangeCanExecute();
            });

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly BindableProperty RetypedPasswordProperty =
            BindableProperty.Create("RetypedPassword", typeof(string), typeof(ChooseAccountViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseAccountViewModel viewModel = (ChooseAccountViewModel)b;
                viewModel.UpdatePasswordState();
                viewModel.PerformActionCommand.ChangeCanExecute();
            });

        public string RetypedPassword
        {
            get { return (string)GetValue(RetypedPasswordProperty); }
            set { SetValue(RetypedPasswordProperty, value); }
        }

        private void UpdatePasswordState()
        {
            PasswordsDoNotMatch = (Password != RetypedPassword) && CreateNew && !CreateRandomPassword;
        }

        public static readonly BindableProperty PasswordsDoNotMatchProperty =
            BindableProperty.Create("PasswordsDoNotMatch", typeof(bool), typeof(ChooseAccountViewModel), default(bool));

        public bool PasswordsDoNotMatch
        {
            get { return (bool)GetValue(PasswordsDoNotMatchProperty); }
            set { SetValue(PasswordsDoNotMatchProperty, value); }
        }

        public static readonly BindableProperty ActionButtonTextProperty =
            BindableProperty.Create("ActionButtonText", typeof(string), typeof(ChooseAccountViewModel), default(string));

        public string ActionButtonText
        {
            get { return (string)GetValue(ActionButtonTextProperty); }
            set { SetValue(ActionButtonTextProperty, value); }
        }

        public string PasswordHash { get; set; }

        public string PasswordHashMethod { get; set; }

        public LegalIdentity LegalIdentity { get; set; }

        public ICommand SwitchModeCommand { get; }

        public ICommand PerformActionCommand { get; }

        private async Task PerformAction()
        {
            SetIsBusy(PerformActionCommand);
            try
            {
                bool succeeded;
                if (CreateNew)
                {
                    succeeded = await CreateAccount();
                }
                else
                {
                    succeeded = await ConnectToAccount();
                }

                Dispatcher.BeginInvokeOnMainThread(() =>
                {
                    SetIsDone(PerformActionCommand);

                    if (succeeded)
                    {
                        OnStepCompleted(EventArgs.Empty);
                    }
                });
            }
            catch (Exception ex)
            {
                this.LogService.LogException(ex);
                await this.NavigationService.DisplayAlert(ex);
            }
            finally
            {
                BeginInvokeSetIsDone(PerformActionCommand);
            }
        }

        private bool ValidateInput()
        {
            // Ok to 'wait' on, since we're not actually waiting on anything.
            return ValidateInput(false).GetAwaiter().GetResult();
        }

        private async Task<bool> ValidateInput(bool alertUser)
        {
            if (CreateNew)
            {
                if (string.IsNullOrWhiteSpace(CreateNewAccountName))
                {
                    if (alertUser)
                    {
                        await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.AccountNameIsInvalid, AppResources.Ok);
                    }

                    return false;
                }

                if (CreateRandomPassword)
                {
                    return true;
                }

                if (Password != RetypedPassword)
                {
                    if (alertUser)
                    {
                        await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.PasswordsDoNotMatch, AppResources.Ok);
                    }
                    return false;
                }

                return true;
            }

            // Use Existing

            if (string.IsNullOrWhiteSpace(this.TagProfile.Domain))
            {
                if (alertUser)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.DomainNameIsInvalid, AppResources.Ok);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConnectToExistingAccountName))
            {
                if (alertUser)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.AccountNameIsInvalid, AppResources.Ok);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                if (alertUser)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.PasswordIsInvalid, AppResources.Ok);
                }
                return false;
            }

            return true;
        }

        private bool CanPerformAction()
        {
            return !IsBusy && ValidateInput();
        }

        private async Task<bool> CreateAccount()
        {
            try
            {
                string passwordToUse = CreateRandomPassword ? this.authService.CreateRandomPassword() : Password;

                (string hostName, int portNumber) = await this.networkService.GetXmppHostnameAndPort(this.TagProfile.Domain);

                async Task OnConnected(XmppClient client)
                {
                    this.PasswordHash = client.PasswordHash;
                    this.PasswordHashMethod = client.PasswordHashMethod;
                    if (this.TagProfile.NeedsUpdating())
                    {
                        await this.NeuronService.DiscoverServices(client);
                    }
                    this.TagProfile.SetAccount(this.CreateNewAccountName, client.PasswordHash, client.PasswordHashMethod);
                }

                (bool succeeded, string errorMessage) = await this.NeuronService.TryConnectAndCreateAccount(this.TagProfile.Domain, hostName, portNumber, this.CreateNewAccountName, passwordToUse, Constants.LanguageCodes.Default, typeof(App).Assembly, OnConnected);

                if (succeeded)
                {
#if DEBUG
                    if (this.CreateRandomPassword)
                    {
                        await Clipboard.SetTextAsync("Password: " + passwordToUse);
                        await this.NavigationService.DisplayAlert(AppResources.Password, string.Format(AppResources.ThePasswordForTheConnectionIs, passwordToUse), AppResources.Ok);
                        System.Diagnostics.Debug.WriteLine("Username: " + this.CreateNewAccountName);
                        System.Diagnostics.Debug.WriteLine("Password: " + passwordToUse);
                    }
#endif
                    return true;
                }

                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
            }
            catch (Exception ex)
            {
                this.LogService.LogException(ex);
                string userMessage = string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain);
                string message = $"{userMessage}{Environment.NewLine}({ex.Message})";
                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, message, AppResources.Ok);
            }

            return false;
        }

        private async Task<bool> ConnectToAccount()
        {
            try
            {
                string password = Password;

                (string hostName, int portNumber) = await this.networkService.GetXmppHostnameAndPort(this.TagProfile.Domain);

                async Task OnConnected(XmppClient client)
                {
                    this.PasswordHash = client.PasswordHash;
                    this.PasswordHashMethod = client.PasswordHashMethod;

                    if (this.TagProfile.LegalIdentityNeedsUpdating())
                    {
                        DateTime now = DateTime.Now;
                        LegalIdentity createdIdentity = null;
                        LegalIdentity approvedIdentity = null;

                        bool serviceDiscoverySucceeded;
                        if (this.TagProfile.NeedsUpdating())
                        {
                            serviceDiscoverySucceeded = await this.NeuronService.DiscoverServices(client);
                        }
                        else
                        {
                            serviceDiscoverySucceeded = true;
                        }

                        if (serviceDiscoverySucceeded)
                        {
                            foreach (LegalIdentity identity in await this.NeuronService.Contracts.GetLegalIdentitiesAsync(client))
                            {
                                if (identity.HasClientSignature &&
                                    identity.HasClientPublicKey &&
                                    identity.From <= now &&
                                    identity.To >= now &&
                                    (identity.State == IdentityState.Approved || identity.State == IdentityState.Created) &&
                                    identity.ValidateClientSignature())
                                {
                                    if (identity.State == IdentityState.Approved)
                                    {
                                        approvedIdentity = identity;
                                        break;
                                    }
                                    if (createdIdentity is null)
                                    {
                                        createdIdentity = identity;
                                    }
                                }
                            }

                            if (approvedIdentity != null)
                            {
                                this.LegalIdentity = approvedIdentity;
                            }
                            else if (createdIdentity != null)
                            {
                                this.LegalIdentity = createdIdentity;
                            }

                            if (this.LegalIdentity != null)
                            {
                                this.TagProfile.SetAccountAndLegalIdentity(this.ConnectToExistingAccountName, client.PasswordHash, client.PasswordHashMethod, this.LegalIdentity);
                            }
                            else
                            {
                                this.TagProfile.SetAccount(this.ConnectToExistingAccountName, client.PasswordHash, client.PasswordHashMethod);
                            }
                        }
                    }
                    else
                    {
                        this.TagProfile.SetAccount(this.ConnectToExistingAccountName, client.PasswordHash, client.PasswordHashMethod);
                    }
                }

                (bool succeeded, string errorMessage) = await this.NeuronService.TryConnectAndConnectToAccount(this.TagProfile.Domain, hostName, portNumber, this.ConnectToExistingAccountName, password, Constants.LanguageCodes.Default, typeof(App).Assembly, OnConnected);

                if (!succeeded)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
                }

                return succeeded;
            }
            catch (Exception ex)
            {
                this.LogService.LogException(ex);
                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain), AppResources.Ok);
            }

            return false;
        }

        protected override async Task DoSaveState()
        {
            await base.DoSaveState();
            this.SettingsService.SaveState(GetSettingsKey(nameof(CreateNewAccountName)), this.CreateNewAccountName);
            this.SettingsService.SaveState(GetSettingsKey(nameof(ConnectToExistingAccountName)), this.ConnectToExistingAccountName);
        }

        protected override async Task DoRestoreState()
        {
            this.CreateNewAccountName = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(CreateNewAccountName)));
            this.ConnectToExistingAccountName = this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(ConnectToExistingAccountName)));
            await base.DoRestoreState();
        }
    }

    public enum AccountMode
    {
        Create,
        Connect
    }
}