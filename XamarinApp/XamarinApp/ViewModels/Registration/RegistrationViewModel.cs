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
        private readonly IMessageService messageService;
        private readonly IAuthService authService;
        private readonly IContractsService contractsService;
        private readonly INavigationService navigationService;
        
        public RegistrationViewModel()
            : this(null, null, null, null, null, null, null)
        {
        }

        // For unit tests
        protected internal RegistrationViewModel(TagProfile tagProfile, ISettingsService settingsService, INeuronService neuronService, IAuthService authService, IMessageService messageService, IContractsService contractsService, INavigationService navigationService)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<TagProfile>();
            this.settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            this.messageService = messageService ?? DependencyService.Resolve<IMessageService>();
            this.authService = authService ?? DependencyService.Resolve<IAuthService>();
            this.contractsService = contractsService ?? DependencyService.Resolve<IContractsService>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            GoToNextCommand = new Command(() => CurrentStep++, () => (RegistrationStep)CurrentStep < RegistrationStep.Pin);
            GoToPrevCommand = new Command(() => CurrentStep--, () => (RegistrationStep)CurrentStep > RegistrationStep.Operator);
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>();
            RegistrationSteps.Add(new ChooseOperatorViewModel(this.tagProfile, this.neuronService, this.messageService));
            RegistrationSteps.Add(new ChooseAccountViewModel(this.tagProfile, this.neuronService, this.messageService, this.authService, this.contractsService));
            RegistrationSteps.Add(new RegisterIdentityViewModel(this.tagProfile, this.neuronService, this.messageService, this.contractsService));
            RegistrationSteps.Add(new ValidateIdentityViewModel(this.tagProfile, this.neuronService, this.messageService, this.contractsService, this.navigationService));
            RegistrationSteps.Add(new DefinePinViewModel(this.tagProfile, this.neuronService, this.messageService));

            RegistrationSteps.ForEach(x => x.StepCompleted += RegistrationStep_Completed);

            this.CurrentStep = (int)DefaultStep;
        }

        public ObservableCollection<RegistrationStepViewModel> RegistrationSteps { get; private set; }

        public ICommand GoToPrevCommand { get; }
        public ICommand GoToNextCommand { get; }

        public static readonly BindableProperty CurrentStepProperty =
            BindableProperty.Create("CurrentStep", typeof(int), typeof(RegistrationViewModel), default(int));

        public int CurrentStep
        {
            get { return (int)GetValue(CurrentStepProperty); }
            set { SetValue(CurrentStepProperty, value); }
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
                        this.navigationService.Set(new MainPage());
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

        // TODO: uncomment when done testing

        //public override async Task RestoreState()
        //{
        //    CurrentStep = await this.settingsService.RestoreState<int>(CurrentStepKey, (int)DefaultStep);
        //}

        public override async Task SaveState()
        {
            await this.settingsService.SaveState(CurrentStepKey, CurrentStep);
        }
    }
}