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
        private static readonly string CurrentStepKey = $"{typeof(RegistrationViewModel).FullName}.CurrentStep";
        private const RegistrationStep DefaultStep = RegistrationStep.Operator;

        private readonly TagProfile tagProfile;
        private readonly ISettingsService settingsService;
        private readonly INeuronService neuronService;
        private readonly IAuthService authService;
        private readonly IContractsService contractsService;
        private readonly INavigationService navigationService;
        
        public RegistrationViewModel()
            : this(null, null, null, null, null, null)
        {
        }

        // For unit tests
        protected internal RegistrationViewModel(TagProfile tagProfile, ISettingsService settingsService, INeuronService neuronService, IAuthService authService, IContractsService contractsService, INavigationService navigationService)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<TagProfile>();
            this.settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            this.authService = authService ?? DependencyService.Resolve<IAuthService>();
            this.contractsService = contractsService ?? DependencyService.Resolve<IContractsService>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            GoToNextCommand = new Command(GoToNext, () => (RegistrationStep)CurrentStep < RegistrationStep.Pin);
            GoToPrevCommand = new Command(GoToPrev, () => (RegistrationStep)CurrentStep > RegistrationStep.Operator);
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>();
            RegistrationSteps.Add(this.AddChildViewModel(new ChooseOperatorViewModel(this.tagProfile, this.neuronService, this.navigationService)));
            RegistrationSteps.Add(this.AddChildViewModel(new ChooseAccountViewModel(this.tagProfile, this.neuronService, this.navigationService, this.authService, this.contractsService)));
            RegistrationSteps.Add(this.AddChildViewModel(new RegisterIdentityViewModel(this.tagProfile, this.neuronService, this.navigationService, this.contractsService)));
            RegistrationSteps.Add(this.AddChildViewModel(new ValidateIdentityViewModel(this.tagProfile, this.neuronService, this.contractsService, this.navigationService)));
            RegistrationSteps.Add(this.AddChildViewModel(new DefinePinViewModel(this.tagProfile, this.neuronService, this.navigationService)));

            RegistrationSteps.ForEach(x => x.StepCompleted += RegistrationStep_Completed);

            this.CurrentStep = (int)DefaultStep;
            UpdateStepTitle();
        }

        public ObservableCollection<RegistrationStepViewModel> RegistrationSteps { get; private set; }

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
                    }
                    break;

                case RegistrationStep.RegisterIdentity:
                    {
                        RegisterIdentityViewModel vm = (RegisterIdentityViewModel)sender;
                        this.tagProfile.SetLegalIdentity(vm.LegalIdentity);
                        GoToNextCommand.Execute();
                    }
                    break;

                case RegistrationStep.ValidateIdentity:
                    {
                        ValidateIdentityViewModel vm = (ValidateIdentityViewModel)sender;
                        this.tagProfile.SetLegalJId(vm.LegalId);
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

        protected override async Task DoRestoreState()
        {
            CurrentStep = await this.settingsService.RestoreState<int>(CurrentStepKey, (int)DefaultStep);
        }

        protected override async Task DoSaveState()
        {
            await this.settingsService.SaveState(CurrentStepKey, CurrentStep);
        }
    }
}