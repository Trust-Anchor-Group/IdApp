using System;
using System.Threading.Tasks;
using System.Windows.Input;
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

        public ChooseAccountViewModel(int step, TagServiceSettings tagSettings, ITagService tagService, IAuthService authService, IMessageService messageService)
            : base(step, tagSettings, tagService)
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
            get { return (bool)GetValue(CreateRandomPasswordProperty); }
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
            get { return (string)GetValue(AccountNameProperty); }
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
            // Do a begin invoke here to break the call chain, this allows for the UI to update its state correctly before performing the operation.
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
            try
            {
                string password = CreateRandomPassword ? this.authService.CreateRandomPassword() : Password;

                (string hostName, int portNumber) = await TagSettings.GetXmppHostnameAndPort();

                Task OnConnected(XmppClient client)
                {
                    if (TagSettings.Step == 1)
                        TagSettings.IncrementConfigurationStep();

                    TagSettings.SetAccount(this.AccountName, client.PasswordHash, client.PasswordHashMethod);
                    return Task.CompletedTask;
                }

                (bool succeeded, string errorMessage) = await this.TagService.TryConnect(this.TagSettings.Domain, hostName, portNumber, this.AccountName, password, string.Empty, string.Empty, typeof(App).Assembly, OnConnected);

                if (succeeded)
                {
                    if (this.CreateRandomPassword)
                        await this.messageService.DisplayAlert(AppResources.Password, string.Format(AppResources.ThePasswordForTheConnectionIs, Password), AppResources.Ok);

                    await App.ShowPage();
                }
                else
                {
                    await this.messageService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
                }
            }
            catch (Exception)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, DomainName), AppResources.Ok);
            }
        }

        private async Task ConnectToAccount()
        {
            try
            {
                string password = Password;

                (string hostName, int portNumber) = await TagSettings.GetXmppHostnameAndPort();

                async Task OnConnected(XmppClient client)
                {
                    if (TagSettings.Step == 1)
                        TagSettings.IncrementConfigurationStep();

                    TagSettings.SetAccount(this.AccountName, client.PasswordHash, client.PasswordHashMethod);

                    if (!TagSettings.LegalIdentityIsValid)
                    {
                        DateTime Now = DateTime.Now;
                        LegalIdentity Created = null;
                        LegalIdentity Approved = null;
                        bool Changed = false;

                        if (!string.IsNullOrEmpty(TagSettings.LegalJid) || await TagService.DiscoverServices(client))
                        {
                            foreach (LegalIdentity Identity in await TagService.GetLegalIdentitiesAsync())
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
                                TagSettings.LegalIdentity = Approved;
                                Changed = true;
                            }
                            else if (!(Created is null))
                            {
                                TagSettings.LegalIdentity = Created;
                                Changed = true;
                            }

                            if (Changed)
                            {
                                if (TagSettings.Step == 2)
                                    TagSettings.IncrementConfigurationStep();
                            }
                        }
                    }
                }

                (bool succeeded, string errorMessage) = await this.TagService.TryConnect(this.TagSettings.Domain, hostName, portNumber, this.AccountName, password, string.Empty, string.Empty, typeof(App).Assembly, OnConnected);

                if (!succeeded)
                {
                    await this.messageService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
                }
            }
            catch (Exception)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, DomainName), AppResources.Ok);
            }
        }
    }
}