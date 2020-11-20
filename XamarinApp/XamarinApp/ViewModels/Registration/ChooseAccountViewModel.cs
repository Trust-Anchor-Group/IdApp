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
        private readonly IAuthService authService;

        public ChooseAccountViewModel(RegistrationStep step, TagProfile tagProfile, ITagService tagService, IMessageService messageService, IAuthService authService)
            : base(step, tagProfile, tagService, messageService)
        {
            this.authService = authService;
            IntroText = string.Format(AppResources.ToConnectToDomainYouNeedAnAccount, this.TagProfile.Domain);
            DomainName = TagProfile.Domain;
            AccountName = TagProfile.Account;
            ActionButtonText = AppResources.CreateNew;
            CreateNew = true;
            CreateRandomPassword = true;
            PerformActionCommand = new Command(async _ => await PerformAction(), _ => CanPerformAction());
            SwitchModeCommand = new Command(_ =>
            {
                CreateNew = !CreateNew;
                PerformActionCommand.ChangeCanExecute();
            });
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
            BindableProperty.Create("CreateRandomPassword", typeof(bool), typeof(ChooseAccountViewModel), default(bool), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseAccountViewModel viewModel = (ChooseAccountViewModel)b;
                viewModel.PerformActionCommand.ChangeCanExecute();
            });

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

        public string PasswordHash { get; set; }

        public string PasswordHashMethod { get; set; }

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
                await this.MessageService.DisplayAlert(ex);
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
                        await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.AccountNameIsInvalid, AppResources.Ok);
                    }

                    return false;
                }

                if (CreateRandomPassword)
                {
                    return true;
                }

                if ((Password != RetypedPassword) || string.IsNullOrWhiteSpace(Password))
                {
                    if (alertUser)
                    {
                        await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.PasswordsDoNotMatch, AppResources.Ok);
                    }
                    return false;
                }

                return true;
            }

            if (string.IsNullOrWhiteSpace(DomainName))
            {
                if (alertUser)
                {
                    await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.DomainNameIsInvalid, AppResources.Ok);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(AccountName))
            {
                if (alertUser)
                {
                    await this.MessageService.DisplayAlert(AppResources.ErrorTitle, AppResources.AccountNameIsInvalid, AppResources.Ok);
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

                (bool succeeded, string errorMessage) = await this.TagService.TryConnectAndCreateAccount(this.TagProfile.Domain, hostName, portNumber, this.AccountName, passwordToUse, Constants.LanguageCodes.Default, typeof(App).Assembly, OnConnected);

                if (succeeded)
                {
                    if (this.CreateRandomPassword)
                        await this.MessageService.DisplayAlert(AppResources.Password, string.Format(AppResources.ThePasswordForTheConnectionIs, passwordToUse), AppResources.Ok);
                    return true;
                }
                else
                {
                    await this.MessageService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
                }
            }
            catch (Exception)
            {
                await this.MessageService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, DomainName), AppResources.Ok);
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

                        if (!string.IsNullOrEmpty(this.TagProfile.LegalJid) || await this.TagService.DiscoverServices(client))
                        {
                            foreach (LegalIdentity identity in await this.TagService.GetLegalIdentitiesAsync())
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

                            if (!(approvedIdentity is null))
                            {
                                this.TagProfile.SetLegalIdentity(approvedIdentity);
                            }
                            else if (!(createdIdentity is null))
                            {
                                this.TagProfile.SetLegalIdentity(createdIdentity);
                            }
                        }
                    }
                }

                (bool succeeded, string errorMessage) = await this.TagService.TryConnectAndConnectToAccount(this.TagProfile.Domain, hostName, portNumber, this.AccountName, password, Constants.LanguageCodes.Default, typeof(App).Assembly, OnConnected);

                if (!succeeded)
                {
                    await this.MessageService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
                }

                return succeeded;
            }
            catch (Exception)
            {
                await this.MessageService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, DomainName), AppResources.Ok);
            }

            return false;
        }
    }
}