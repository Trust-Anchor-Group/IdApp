using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class ChooseOperatorViewModel : RegistrationStepViewModel
    {
        private readonly INetworkService networkService;
        private string hostName;
        private int portNumber;

        public ChooseOperatorViewModel(
            TagProfile tagProfile, 
            INeuronService neuronService, 
            INavigationService navigationService,
            ISettingsService settingsService,
            INetworkService networkService,
            ILogService logService)
            : base(RegistrationStep.Operator, tagProfile, neuronService, navigationService, settingsService, logService)
        {
            this.networkService = networkService;
            this.Operators = new ObservableCollection<string>();
            this.ConnectCommand = new Command(async () => await Connect(), ConnectCanExecute);
            this.ManualOperatorCommand = new Command<string>(async text => await ManualOperatorTextEdited(text));
            this.PopulateOperators();
            this.Title = AppResources.SelectOperator;
        }

        public ObservableCollection<string> Operators { get; }

        public static readonly BindableProperty SelectedOperatorProperty =
            BindableProperty.Create("SelectedOperator", typeof(string), typeof(ChooseOperatorViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseOperatorViewModel viewModel = (ChooseOperatorViewModel)b;
                string selectedOperator = (string)newValue;
                viewModel.ChooseOperatorFromList = (viewModel.Operators.Count > 0) && (selectedOperator != AppResources.OperatorDomainOther);
                viewModel.ConnectCommand.ChangeCanExecute();
            });

        public string SelectedOperator
        {
            get { return (string)GetValue(SelectedOperatorProperty); }
            set { SetValue(SelectedOperatorProperty, value); }
        }

        public static readonly BindableProperty ManualOperatorProperty =
            BindableProperty.Create("ManualOperator", typeof(string), typeof(ChooseOperatorViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ChooseOperatorViewModel viewModel = (ChooseOperatorViewModel)b;
                viewModel.ConnectCommand.ChangeCanExecute();
            });

        public string ManualOperator
        {
            get { return (string)GetValue(ManualOperatorProperty); }
            set { SetValue(ManualOperatorProperty, value); }
        }

        public static readonly BindableProperty ChooseOperatorFromListProperty =
            BindableProperty.Create("ChooseOperatorFromList", typeof(bool), typeof(ChooseOperatorViewModel), default(bool));

        public bool ChooseOperatorFromList
        {
            get { return (bool)GetValue(ChooseOperatorFromListProperty); }
            set { SetValue(ChooseOperatorFromListProperty, value); }
        }

        public string GetOperator()
        {
            return !string.IsNullOrWhiteSpace(SelectedOperator) ? SelectedOperator : ManualOperator;
        }

        public ICommand ConnectCommand { get; }

        private async Task Connect()
        {
            SetIsBusy(ConnectCommand);

            try
            {
                string domainName = GetOperator();
                (this.hostName, this.portNumber) = await this.networkService.GetXmppHostnameAndPort(domainName);

                (bool succeeded, string errorMessage) = await this.NeuronService.TryConnect(domainName, hostName, portNumber, Constants.LanguageCodes.Default, typeof(App).Assembly, null);

                Dispatcher.BeginInvokeOnMainThread(async () =>
                {
                    this.SetIsDone(ConnectCommand);

                    if (succeeded)
                    {
                        this.TagProfile.SetDomain(this.GetOperator());
                        this.OnStepCompleted(EventArgs.Empty);
                    }
                    else
                    {
                        this.LogService.LogException(new InvalidOperationException(), new KeyValuePair<string, string>("Connect", "Failed to connect"));
                        await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
                    }
                });
            }
            catch(Exception ex)
            {
                this.LogService.LogException(ex);
                await this.NavigationService.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
            }
            finally
            {
                this.BeginInvokeSetIsDone(ConnectCommand);
            }

        }

        private bool ConnectCanExecute()
        {
            if (IsBusy) // is connecting
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(this.SelectedOperator) && this.Operators.Contains(this.SelectedOperator) && this.SelectedOperator != AppResources.OperatorDomainOther)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(ManualOperator))
            {
                return true;
            }

            return false;
        }

        public ICommand ManualOperatorCommand { get; }

        private async Task ManualOperatorTextEdited(string text)
        {
            if (await IsValidDomainName(text))
            {
                ManualOperator = text;
            }
            else
            {
                ManualOperator = string.Empty;
            }
        }

        private async Task<bool> IsValidDomainName(string name)
        {
            string[] parts = name.Split('.');
            if (parts.Length <= 1)
                return false;

            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    return false;

                foreach (char ch in part)
                {
                    if (!char.IsLetter(ch) && !char.IsDigit(ch) && ch != '-')
                        return false;
                }
            }

            (string host, int port) = await this.networkService.GetXmppHostnameAndPort(name);

            if (string.IsNullOrWhiteSpace(host))
                return false;

            this.hostName = host;
            this.portNumber = port;

            return true;
        }

        private void PopulateOperators()
        {
            int selectedIndex = -1;
            int i = 0;
            foreach (string domain in this.TagProfile.Domains)
            {
                this.Operators.Add(domain);

                if (domain == this.TagProfile.Domain)
                {
                    selectedIndex = i;
                }
                i++;
            }
            this.Operators.Add(AppResources.OperatorDomainOther);

            if (selectedIndex >= 0)
            {
                this.SelectedOperator = this.Operators[selectedIndex];
            }

            if (!string.IsNullOrWhiteSpace(this.TagProfile.Domain))
            {
                if (selectedIndex >= 0)
                {
                    this.SelectedOperator = this.Operators[selectedIndex];
                }
                else
                {
                    this.ManualOperator = this.TagProfile.Domain;
                }
            }
            else
            {
                SelectedOperator = string.Empty;
            }
        }
    }
}