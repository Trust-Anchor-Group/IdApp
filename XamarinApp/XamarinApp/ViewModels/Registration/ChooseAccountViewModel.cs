using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class ChooseAccountViewModel : RegistrationStepViewModel
    {
        private readonly IMessageService messageService;
        private readonly IAuthService authService;

        public ChooseAccountViewModel(int step, ITagService tagService, IAuthService authService, IMessageService messageService)
            : base(step, tagService)
        {
            this.messageService = messageService;
            this.authService = authService;
            IntroText = string.Format(AppResources.ToConnectToDomainYouNeedAnAccount, this.TagService.Domain);
            DomainName = TagService.Domain;
            AccountName = TagService.Account;
            CreateNew = true;
            CreateRandomPassword = true;
            PerformActionCommand = new Command(_ => PerformAction(), _ => CanPerformAction());
            SwitchModeCommand = new Command<bool>(b => CreateNew = b);
            AccountNameCommand = new Command(() => { });
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
            });

        public bool CreateNew
        {
            get { return (bool)GetValue(CreateNewProperty); }
            set { SetValue(CreateNewProperty, value); }
        }

        public static readonly BindableProperty CreateRandomPasswordProperty =
            BindableProperty.Create("CreateRandomPassword", typeof(bool), typeof(ChooseAccountViewModel), default(bool));

        public bool CreateRandomPassword
        {
            get { return (bool) GetValue(CreateRandomPasswordProperty); }
            set { SetValue(CreateRandomPasswordProperty, value); }
        }

        public static readonly BindableProperty DomainNameProperty =
            BindableProperty.Create("DomainName", typeof(string), typeof(ChooseAccountViewModel), default(string));

        public string DomainName
        {
            get { return (string)GetValue(DomainNameProperty); }
            set { SetValue(DomainNameProperty, value); }
        }

        public static readonly BindableProperty AccountNameProperty =
            BindableProperty.Create("AccountName", typeof(string), typeof(ChooseAccountViewModel), default(string));

        public string AccountName
        {
            get { return (string) GetValue(AccountNameProperty); }
            set { SetValue(AccountNameProperty, value); }
        }

        public static readonly BindableProperty PasswordProperty =
            BindableProperty.Create("Password", typeof(string), typeof(ChooseAccountViewModel), default(string));

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly BindableProperty RetypedPasswordProperty =
            BindableProperty.Create("RetypedPassword", typeof(string), typeof(ChooseAccountViewModel), default(string));

        public string RetypedPassword
        {
            get { return (string)GetValue(RetypedPasswordProperty); }
            set { SetValue(RetypedPasswordProperty, value); }
        }

        public static readonly BindableProperty ActionButtonTextProperty =
            BindableProperty.Create("ActionButtonText", typeof(string), typeof(ChooseAccountViewModel), default(string));

        public string ActionButtonText
        {
            get { return (string)GetValue(ActionButtonTextProperty); }
            set { SetValue(ActionButtonTextProperty, value); }
        }

        public ICommand AccountNameCommand { get; }

        public ICommand SwitchModeCommand { get; }

        public ICommand PerformActionCommand { get; }

        private void PerformAction()
        {
            IsBusy = true;
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (CreateNew)
                    {
                        await CreateAccount();
                    }
                    else
                    {
                        await ConnectToAccount();
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            });
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
                if (!CreateRandomPassword && Password != RetypedPassword)
                {
                    if (alertUser)
                    {
                        await this.messageService.DisplayAlert(AppResources.ErrorTitle, AppResources.PasswordsDoNotMatch, AppResources.Ok);
                    }
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(DomainName))
            {
                if (alertUser)
                {
                    await this.messageService.DisplayAlert(AppResources.ErrorTitle, AppResources.DomainNameIsInvalid, AppResources.Ok);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(AccountName))
            {
                if (alertUser)
                {
                    await this.messageService.DisplayAlert(AppResources.ErrorTitle, AppResources.AccountNameIsInvalid, AppResources.Ok);
                }
                return false;
            }

            return true;
        }

        private bool CanPerformAction()
        {
            return !IsBusy && ValidateInput();
        }

        private async Task CreateAccount()
        {
            string password = CreateRandomPassword ? this.authService.CreateRandomPassword() : Password;

            try
            {
                (string HostName, int PortNumber) = await TagService.GetXmppHostnameAndPort(TagService.Configuration.Domain);

                using (XmppClient Client = TagService.CreateClient(HostName, PortNumber, this.AccountName, password, string.Empty, string.Empty, typeof(App).Assembly))
                {
                    TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>();
                    bool StreamNegotiation = false;
                    bool StreamOpened = false;
                    bool StartingEncryption = false;
                    bool Authentication = false;
                    bool Registering = false;
                    bool Timeout = false;

                    if (XmppConfiguration.TryGetKeys(TagService.Configuration.Domain, out string Key, out string Secret))
                        Client.AllowRegistration(Key, Secret);
                    else
                        Client.AllowRegistration();

                    Client.TrustServer = false;
                    Client.AllowCramMD5 = false;
                    Client.AllowDigestMD5 = false;
                    Client.AllowPlain = false;
                    Client.AllowEncryption = true;
                    Client.AllowScramSHA1 = true;

                    Client.OnStateChanged += (sender2, NewState) =>
                    {
                        switch (NewState)
                        {
                            case XmppState.StreamNegotiation:
                                StreamNegotiation = true;
                                break;

                            case XmppState.StreamOpened:
                                StreamOpened = true;
                                break;

                            case XmppState.StartingEncryption:
                                StartingEncryption = true;
                                break;

                            case XmppState.Authenticating:
                                Authentication = true;
                                break;

                            case XmppState.Registering:
                                Registering = true;
                                break;

                            case XmppState.Connected:
                                Result.TrySetResult(true);
                                break;

                            case XmppState.Offline:
                            case XmppState.Error:
                                Result.TrySetResult(false);
                                break;
                        }

                        return Task.CompletedTask;
                    };

                    Client.Connect(TagService.Configuration.Domain);

                    bool Success;

                    using (Timer Timer = new Timer((P) =>
                    {
                        Timeout = true;
                        Result.TrySetResult(false);
                    }, null, 10000, 0))
                    {
                        Success = await Result.Task;
                    }

                    if (Success)
                    {
                        if (TagService.Configuration.Step == 1)
                            TagService.IncrementConfigurationStep();

                        TagService.SetAccount(this.AccountName, Client.PasswordHash, Client.PasswordHashMethod);

                        if (this.CreateRandomPassword)
                            await this.messageService.DisplayAlert("Password", "The password for the connection is " + Password, AppResources.Ok);

                        await App.ShowPage();
                    }
                    else
                    {
                        if (!StreamNegotiation || Timeout)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, "Cannot connect to " + TagService.Configuration.Domain, AppResources.Ok);
                        else if (!StreamOpened)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, TagService.Configuration.Domain + " is not a valid operator.", AppResources.Ok);
                        else if (!StartingEncryption)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, TagService.Configuration.Domain + " does not follow the ubiquitous encryption policy.", AppResources.Ok);
                        else if (!Authentication)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, "Unable to authentication with " + TagService.Configuration.Domain + ".", AppResources.Ok);
                        else if (!Registering)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, "The operator " + TagService.Configuration.Domain + " does not support registration of new accounts.", AppResources.Ok);
                        else
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, "Account name already taken. Choose another.", AppResources.Ok);
                    }
                }
            }
            catch (Exception)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitle, "Unable to connect to " + TagService.Configuration.Domain, AppResources.Ok);
            }
        }

        private async Task ConnectToAccount()
        {
            try
            {
                string password = Password;
                bool Success = false;

                (string HostName, int PortNumber) = await TagService.GetXmppHostnameAndPort();

                using (XmppClient Client = TagService.CreateClient(HostName, PortNumber, this.AccountName, password, string.Empty, string.Empty, typeof(App).Assembly))
                {
                    TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>();
                    bool StreamNegotiation = false;
                    bool StreamOpened = false;
                    bool StartingEncryption = false;
                    bool Authentication = false;
                    bool Timeout = false;

                    Client.TrustServer = false;
                    Client.AllowCramMD5 = false;
                    Client.AllowDigestMD5 = false;
                    Client.AllowPlain = false;
                    Client.AllowEncryption = true;
                    Client.AllowScramSHA1 = true;

                    Client.OnStateChanged += (sender2, NewState) =>
                    {
                        switch (NewState)
                        {
                            case XmppState.StreamNegotiation:
                                StreamNegotiation = true;
                                break;

                            case XmppState.StreamOpened:
                                StreamOpened = true;
                                break;

                            case XmppState.StartingEncryption:
                                StartingEncryption = true;
                                break;

                            case XmppState.Authenticating:
                                Authentication = true;
                                break;

                            case XmppState.Connected:
                                Result.TrySetResult(true);
                                break;

                            case XmppState.Offline:
                            case XmppState.Error:
                                Result.TrySetResult(false);
                                break;
                        }

                        return Task.CompletedTask;
                    };

                    Client.Connect(TagService.Configuration.Domain);

                    using (Timer _ = new Timer((P) =>
                    {
                        Timeout = true;
                        Result.TrySetResult(false);
                    }, null, 10000, 0))
                    {
                        Success = await Result.Task;
                    }

                    if (Success)
                    {
                        if (TagService.Configuration.Step == 1)
                            TagService.Configuration.Step++;

                        TagService.SetAccount(this.AccountName, Client.PasswordHash, Client.PasswordHashMethod);

                        if (!TagService.LegalIdentityIsValid)
                        {
                            DateTime Now = DateTime.Now;
                            LegalIdentity Created = null;
                            LegalIdentity Approved = null;
                            bool Changed = false;

                            if (!string.IsNullOrEmpty(TagService.Configuration.LegalJid) ||
                                await TagService.FindServices(Client))
                            {
                                using (ContractsClient Contracts = await ContractsClient.Create(Client, TagService.Configuration.LegalJid))
                                {
                                    foreach (LegalIdentity Identity in await Contracts.GetLegalIdentitiesAsync())
                                    {
                                        if (Identity.HasClientSignature &&
                                            Identity.HasClientPublicKey &&
                                            Identity.From <= Now &&
                                            Identity.To >= Now &&
                                            (Identity.State == IdentityState.Approved || Identity.State == IdentityState.Created) &&
                                            Identity.ValidateClientSignature())
                                        {
                                            if (Identity.State == IdentityState.Approved)
                                            {
                                                Approved = Identity;
                                                break;
                                            }
                                            else if (Created is null)
                                                Created = Identity;
                                        }
                                    }

                                    if (!(Approved is null))
                                    {
                                        TagService.Configuration.LegalIdentity = Approved;
                                        Changed = true;
                                    }
                                    else if (!(Created is null))
                                    {
                                        TagService.Configuration.LegalIdentity = Created;
                                        Changed = true;
                                    }

                                    if (Changed)
                                    {
                                        if (TagService.Configuration.Step == 2)
                                            TagService.IncrementConfigurationStep();

                                        TagService.UpdateConfiguration();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!StreamNegotiation || Timeout)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.CantConnectTo, TagService.Configuration.Domain), AppResources.Ok);
                        else if (!StreamOpened)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.DomainIsNotAValidOperator, TagService.Configuration.Domain), AppResources.Ok);
                        else if (!StartingEncryption)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.DomainDoesNotFollowEncryptionPolicy, TagService.Configuration.Domain), AppResources.Ok);
                        else if (!Authentication)
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, TagService.Configuration.Domain), AppResources.Ok);
                        else
                            await this.messageService.DisplayAlert(AppResources.ErrorTitle, AppResources.InvalidUsernameOrPassword, "Invalid user name or password.", AppResources.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitle, $"Unable to connect to {TagService.Configuration.Domain}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{ex.Message}", AppResources.Ok);
            }

        }
    }
}