using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class ChooseAccountViewModel : RegistrationStepViewModel
    {
        private readonly IContractsService contractsService;
        private readonly IAuthService authService;

        public ChooseAccountViewModel(
            TagProfile tagProfile, 
            INeuronService neuronService, 
            INavigationService navigationService,
            ISettingsService settingsService,
            IAuthService authService, 
            IContractsService contractsService)
            : base(RegistrationStep.Account, tagProfile, neuronService, navigationService, settingsService)
        {
            this.authService = authService;
            this.contractsService = contractsService;
            this.PerformActionCommand = new Command(async _ => await PerformAction(), _ => CanPerformAction());
            this.SwitchModeCommand = new Command(_ =>
            {
                CreateNew = !CreateNew;
                PerformActionCommand.ChangeCanExecute();
            });
            this.ActionButtonText = AppResources.CreateNew;
            this.CreateNew = true;
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

        private void TagProfile_Changed(object sender, EventArgs e)
        {
            IntroText = string.Format(AppResources.ToConnectToDomainYouNeedAnAccount, this.TagProfile.Domain);
            AccountName = this.TagProfile.Account;
        }

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

        public static readonly BindableProperty AccountNameProperty =
            BindableProperty.Create("AccountName", typeof(string), typeof(ChooseAccountViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseAccountViewModel viewModel = (ChooseAccountViewModel)b;
                viewModel.PerformActionCommand.ChangeCanExecute();
            });

        public string AccountName
        {
            get { return (string)GetValue(AccountNameProperty); }
            set { SetValue(AccountNameProperty, value); }
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
            IsBusy = true;

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

                Device.BeginInvokeOnMainThread(() =>
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
                if (string.IsNullOrWhiteSpace(AccountName))
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

            if (string.IsNullOrWhiteSpace(AccountName))
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

                (string hostName, int portNumber) = await TagProfile.GetXmppHostnameAndPort();

                Task OnConnected(XmppClient client)
                {
                    this.PasswordHash = client.PasswordHash;
                    this.PasswordHashMethod = client.PasswordHashMethod;
                    return Task.CompletedTask;
                }

                (bool succeeded, string errorMessage) = await this.NeuronService.TryConnectAndCreateAccount(this.TagProfile.Domain, hostName, portNumber, this.AccountName, passwordToUse, Constants.LanguageCodes.Default, typeof(App).Assembly, OnConnected);

                if (succeeded)
                {
                    if (this.CreateRandomPassword)
                    {
                        await this.NavigationService.DisplayAlert(AppResources.Password, string.Format(AppResources.ThePasswordForTheConnectionIs, passwordToUse), AppResources.Ok);
                        System.Diagnostics.Debug.WriteLine("Username: " + this.AccountName);
                        System.Diagnostics.Debug.WriteLine("Password: " + passwordToUse);
                    }
                    return true;
                }

                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
            }
            catch (Exception)
            {
                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain), AppResources.Ok);
            }

            return false;
        }

        private async Task<bool> ConnectToAccount()
        {
            try
            {
                string password = Password;

                (string hostName, int portNumber) = await TagProfile.GetXmppHostnameAndPort();

                async Task OnConnected(XmppClient client)
                {
                    this.TagProfile.SetAccount(this.AccountName, client.PasswordHash, client.PasswordHashMethod);

                    if (!this.TagProfile.LegalIdentity.NeedsUpdating())
                    {
                        DateTime now = DateTime.Now;
                        LegalIdentity createdIdentity = null;
                        LegalIdentity approvedIdentity = null;

                        if (!string.IsNullOrEmpty(this.TagProfile.LegalJid) || await this.NeuronService.DiscoverServices(client))
                        {
                            foreach (LegalIdentity identity in await this.contractsService.GetLegalIdentitiesAsync())
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
                        }
                    }
                }

                (bool succeeded, string errorMessage) = await this.NeuronService.TryConnectAndConnectToAccount(this.TagProfile.Domain, hostName, portNumber, this.AccountName, password, Constants.LanguageCodes.Default, typeof(App).Assembly, OnConnected);

                if (!succeeded)
                {
                    await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
                }

                return succeeded;
            }
            catch (Exception)
            {
                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain), AppResources.Ok);
            }

            return false;
        }

        protected override async Task DoSaveState()
        {
            await this.SettingsService.SaveState(GetSettingsKey(nameof(AccountName)), this.AccountName);
            await base.DoSaveState();
        }

        protected override async Task DoRestoreState()
        {
            this.AccountName = await this.SettingsService.RestoreState<string>(GetSettingsKey(nameof(AccountName)));
            await base.DoRestoreState();
        }
    }
}