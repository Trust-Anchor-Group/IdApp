using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationViewModel : BaseViewModel
    {
        private static readonly string CurrentStepKey = $"{typeof(RegistrationViewModel).FullName}.CurrentStep";
        private const RegistrationStep DefaultStep = RegistrationStep.Operator;

        private readonly TagProfile tagProfile;
        private readonly ISettingsService settingsService;
        private readonly ITagService tagService;
        private readonly IMessageService messageService;
        private readonly IAuthService authService;

        public RegistrationViewModel()
            : this(null, null, null, null, null)
        {
        }

        // For unit tests
        protected internal RegistrationViewModel(TagProfile tagProfile, ISettingsService settingsService, ITagService tagService, IAuthService authService, IMessageService messageService)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<TagProfile>();
            this.settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            this.tagService = tagService ?? DependencyService.Resolve<ITagService>();
            this.messageService = messageService ?? DependencyService.Resolve<IMessageService>();
            this.authService = authService ?? DependencyService.Resolve<IAuthService>();
            GoToNextCommand = new Command(() => CurrentStep++, () => (RegistrationStep)CurrentStep < RegistrationStep.Pin);
            GoToPrevCommand = new Command(() => CurrentStep--, () => (RegistrationStep)CurrentStep > RegistrationStep.Operator);
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>();
            RegistrationSteps.Add(new ChooseOperatorViewModel(RegistrationStep.Operator, this.tagProfile, this.tagService, this.messageService));
            RegistrationSteps.Add(new ChooseAccountViewModel(RegistrationStep.Account, this.tagProfile, this.tagService, this.messageService, this.authService));
            RegistrationSteps.Add(new RegisterIdentityViewModel(RegistrationStep.RegisterIdentity, this.tagProfile, this.tagService, this.messageService));
            RegistrationSteps.Add(new RegistrationStepViewModel(RegistrationStep.Identity, this.tagProfile, this.tagService, this.messageService));
            RegistrationSteps.Add(new RegistrationStepViewModel(RegistrationStep.Pin, this.tagProfile, this.tagService, this.messageService));

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

                case RegistrationStep.Identity:
                    {
                        //IdentityViewModel vm = (IdentityViewModel)sender;
                        GoToNextCommand.Execute();
                    }
                    break;

                case RegistrationStep.Pin:
                    {
                        //ChoosePinViewModel vm = (ChoosePinViewModel)sender;
                        GoToNextCommand.Execute();
                    }
                    break;

                default: // RegistrationStep.Operator
                    {
                        ChooseOperatorViewModel vm = (ChooseOperatorViewModel)sender;
                        this.tagProfile.SetDomain(vm.GetOperator(), string.Empty);
                        GoToNextCommand.Execute();
                    }
                    break;
            }
        }

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