using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Extensions;
using XamarinApp.Services;
using XamarinApp.Views;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationViewModel : BaseViewModel
    {
        private const RegistrationStep DefaultStep = RegistrationStep.Operator;
        public event EventHandler<EventArgs> StepChanged; 
        private readonly TagProfile tagProfile;
        private readonly ISettingsService settingsService;
        private readonly ILogService logService;
        private readonly INeuronService neuronService;
        private readonly IAuthService authService;
        private readonly IContractsService contractsService;
        private readonly INavigationService navigationService;
        private readonly INetworkService networkService;
        
        public RegistrationViewModel()
            : this(null, null, null, null, null, null, null)
        {
        }

        // For unit tests
        protected internal RegistrationViewModel(TagProfile tagProfile, ISettingsService settingsService, INeuronService neuronService, IAuthService authService, IContractsService contractsService, INavigationService navigationService, ILogService logService)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<TagProfile>();
            this.settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            this.authService = authService ?? DependencyService.Resolve<IAuthService>();
            this.contractsService = contractsService ?? DependencyService.Resolve<IContractsService>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            this.networkService = networkService ?? DependencyService.Resolve<INetworkService>();
            this.logService = logService ?? DependencyService.Resolve<ILogService>();
            GoToNextCommand = new Command(GoToNext, () => (RegistrationStep)CurrentStep < RegistrationStep.Pin);
            GoToPrevCommand = new Command(GoToPrev, () => (RegistrationStep)CurrentStep > RegistrationStep.Operator);
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>
            {
                this.AddChildViewModel(new ChooseOperatorViewModel(this.tagProfile, this.neuronService, this.navigationService, this.settingsService, this.networkService, this.logService)),
                this.AddChildViewModel(new ChooseAccountViewModel(this.tagProfile, this.neuronService, this.navigationService, this.settingsService, this.authService, this.contractsService, this.networkService, this.logService)),
                this.AddChildViewModel(new RegisterIdentityViewModel(this.tagProfile, this.neuronService, this.navigationService, this.settingsService, this.contractsService, this.logService)),
                this.AddChildViewModel(new ValidateIdentityViewModel(this.tagProfile, this.neuronService, this.navigationService, this.settingsService, this.contractsService, this.logService)),
                this.AddChildViewModel(new DefinePinViewModel(this.tagProfile, this.neuronService, this.navigationService, this.settingsService, this.logService))
            };

            this.CurrentStep = (int)DefaultStep;
            UpdateStepTitle();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            RegistrationSteps.ForEach(x => x.StepCompleted += RegistrationStep_Completed);
            this.CurrentStep = (int)this.tagProfile.Step;
        }

        protected override Task DoUnbind()
        {
            RegistrationSteps.ForEach(x => x.StepCompleted -= RegistrationStep_Completed);
            return base.DoUnbind();
        }

        public ObservableCollection<RegistrationStepViewModel> RegistrationSteps { get; }

        public ICommand GoToPrevCommand { get; }
        public ICommand GoToNextCommand { get; }

        public static readonly BindableProperty CanGoBackProperty =
            BindableProperty.Create("CanGoBack", typeof(bool), typeof(RegistrationViewModel), default(bool));

        public bool CanGoBack
        {
            get { return (bool) GetValue(CanGoBackProperty); }
            set { SetValue(CanGoBackProperty, value); }
        }

        public static readonly BindableProperty CurrentStepProperty =
            BindableProperty.Create("CurrentStep", typeof(int), typeof(RegistrationViewModel), default(int), propertyChanged: (b, oldValue, newValue) =>
            {
                RegistrationViewModel viewModel = (RegistrationViewModel)b;
                viewModel.UpdateStepTitle();
                viewModel.CanGoBack = viewModel.GoToPrevCommand.CanExecute(null);
                Device.BeginInvokeOnMainThread(() => viewModel.StepChanged?.Invoke(viewModel, EventArgs.Empty));
            });

        public int CurrentStep
        {
            get { return (int)GetValue(CurrentStepProperty); }
            set { SetValue(CurrentStepProperty, value); }
        }

        public static readonly BindableProperty CurrentStepTitleProperty =
            BindableProperty.Create("CurrentStepTitle", typeof(string), typeof(RegistrationViewModel), default(string));

        public string CurrentStepTitle
        {
            get { return (string) GetValue(CurrentStepTitleProperty); }
            set { SetValue(CurrentStepTitleProperty, value); }
        }

        private void UpdateStepTitle()
        {
            this.CurrentStepTitle = this.RegistrationSteps[this.CurrentStep].Title;
        }

        private void RegistrationStep_Completed(object sender, EventArgs e)
        {
            RegistrationStep step = ((RegistrationStepViewModel)sender).Step;
            switch (step)
            {
                case RegistrationStep.Account:
                    {
                        ChooseAccountViewModel vm = (ChooseAccountViewModel)sender;
                        this.tagProfile.SetAccount(vm.AccountName, vm.PasswordHash, vm.PasswordHashMethod);
                        if (vm.LegalIdentity != null)
                        {
                            this.tagProfile.SetLegalIdentity(vm.LegalIdentity);
                        }
                        GoToNextCommand.Execute();
                        if (vm.Mode == AccountMode.Connect && vm.LegalIdentity != null)
                        {
                            // Skip Register identity, go directly to Validate identity
                            Dispatcher.BeginInvokeOnMainThread(() => GoToNextCommand.Execute());
                        }
                    }
                    break;

                case RegistrationStep.RegisterIdentity:
                    {
                        RegisterIdentityViewModel vm = (RegisterIdentityViewModel)sender;
                        this.tagProfile.SetLegalIdentity(vm.LegalIdentity); // created
                        GoToNextCommand.Execute();
                    }
                    break;

                case RegistrationStep.ValidateIdentity:
                    {
                        ValidateIdentityViewModel vm = (ValidateIdentityViewModel)sender;
                        this.tagProfile.SetLegalIdentity(vm.LegalIdentity); // validated
                        GoToNextCommand.Execute();
                    }
                    break;

                case RegistrationStep.Pin:
                    {
                        DefinePinViewModel vm = (DefinePinViewModel)sender;
                        this.tagProfile.SetPin(vm.Pin, vm.UsePin);
                        this.navigationService.ReplaceAsync(new MainPage());
                    }
                    break;

                default: // RegistrationStep.Operator
                    {
                        ChooseOperatorViewModel vm = (ChooseOperatorViewModel)sender;
                        this.tagProfile.SetDomain(vm.GetOperator());
                        GoToNextCommand.Execute();
                    }
                    break;
            }
        }

        private void GoToNext()
        {
            CurrentStep++;
        }

        private void GoToPrev()
        {
            RegistrationStep currStep = (RegistrationStep)CurrentStep;

            switch (currStep)
            {
                case RegistrationStep.Account:
                    {
                        this.tagProfile.ClearAccount();
                    }
                    break;

                case RegistrationStep.RegisterIdentity:
                    {
                        this.tagProfile.ClearLegalIdentity();
                    }
                    break;

                case RegistrationStep.ValidateIdentity:
                    {
                        this.tagProfile.ClearLegalJId();
                    }
                    break;

                case RegistrationStep.Pin:
                    {
                        this.tagProfile.ClearPin();
                    }
                    break;

                default: // RegistrationStep.Operator
                    {
                        this.tagProfile.ClearDomain();
                    }
                    break;
            }

            CurrentStep--;
        }
    }
}