using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Services;
using XamarinApp.Views;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationViewModel : BaseViewModel
    {
        private readonly TagProfile tagProfile;
        private readonly INavigationService navigationService;
        
        public RegistrationViewModel()
            : this(null, null, null, null, null, null, null, null)
        {
        }

        // For unit tests
        protected internal RegistrationViewModel(TagProfile tagProfile, ISettingsService settingsService, INeuronService neuronService, IAuthService authService, IContractsService contractsService, INavigationService navigationService, INetworkService networkService, ILogService logService)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<TagProfile>();
            settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            authService = authService ?? DependencyService.Resolve<IAuthService>();
            contractsService = contractsService ?? DependencyService.Resolve<IContractsService>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            networkService = networkService ?? DependencyService.Resolve<INetworkService>();
            logService = logService ?? DependencyService.Resolve<ILogService>();
            GoToPrevCommand = new Command(GoToPrev, () => (RegistrationStep)CurrentStep > RegistrationStep.Operator);
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>
            {
                this.AddChildViewModel(new ChooseOperatorViewModel(this.tagProfile, neuronService, this.navigationService, settingsService, networkService, logService)),
                this.AddChildViewModel(new ChooseAccountViewModel(this.tagProfile, neuronService, this.navigationService, settingsService, authService, contractsService, networkService, logService)),
                this.AddChildViewModel(new RegisterIdentityViewModel(this.tagProfile, neuronService, this.navigationService, settingsService, contractsService, logService)),
                this.AddChildViewModel(new ValidateIdentityViewModel(this.tagProfile, neuronService, this.navigationService, settingsService, contractsService, networkService, logService)),
                this.AddChildViewModel(new DefinePinViewModel(this.tagProfile, neuronService, this.navigationService, settingsService, logService))
            };
            SyncTagProfileStep();
            UpdateStepTitle();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            RegistrationSteps.ForEach(x => x.StepCompleted += RegistrationStep_Completed);
            SyncTagProfileStep();
        }

        protected override Task DoUnbind()
        {
            RegistrationSteps.ForEach(x => x.StepCompleted -= RegistrationStep_Completed);
            return base.DoUnbind();
        }

        public ObservableCollection<RegistrationStepViewModel> RegistrationSteps { get; }

        public ICommand GoToPrevCommand { get; }

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
                    SyncTagProfileStep();
                    break;

                case RegistrationStep.RegisterIdentity:
                    SyncTagProfileStep();
                    break;

                case RegistrationStep.ValidateIdentity:
                    SyncTagProfileStep();
                    break;

                case RegistrationStep.Pin:
                    this.navigationService.ReplaceAsync(new MainPage());
                    break;

                default: // RegistrationStep.Operator
                    SyncTagProfileStep();
                    break;
            }
        }

        private void GoToPrev()
        {
            RegistrationStep currStep = (RegistrationStep)CurrentStep;

            switch (currStep)
            {
                case RegistrationStep.Account:
                    this.tagProfile.ClearAccount();
                    break;

                case RegistrationStep.RegisterIdentity:
                    this.tagProfile.ClearLegalIdentity();
                    break;

                case RegistrationStep.ValidateIdentity:
                    this.tagProfile.ClearIsValidated();
                    break;

                case RegistrationStep.Pin:
                    this.tagProfile.ClearPin();
                    break;

                default: // RegistrationStep.Operator
                    this.tagProfile.ClearDomain();
                    break;
            }

            SyncTagProfileStep();
        }

        private void SyncTagProfileStep()
        {
            this.CurrentStep = (int)this.tagProfile.Step;
        }
    }
}