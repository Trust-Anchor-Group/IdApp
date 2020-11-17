using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationViewModel : BaseViewModel
    {
        private static readonly string CurrentStepKey = $"{typeof(RegistrationViewModel).FullName}.CurrentStep";
        private const int MinStep = 0;
        private const int MaxStep = 4;
        private const int DefaultStep = MinStep;

        private readonly TagServiceSettings tagSettings;
        private readonly ISettingsService settingsService;
        private readonly ITagService tagService;
        private readonly IMessageService messageService;
        private readonly IAuthService authService;

        public RegistrationViewModel(TagServiceSettings tagSettings)
            : this(tagSettings, null, null, null, null)
        {
        }

        // For unit tests
        protected internal RegistrationViewModel(TagServiceSettings tagSettings, ISettingsService settingsService, ITagService tagService, IAuthService authService, IMessageService messageService)
        {
            this.tagSettings = tagSettings;
            this.settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            this.tagService = tagService ?? DependencyService.Resolve<ITagService>();
            this.messageService = messageService ?? DependencyService.Resolve<IMessageService>();
            this.authService = authService ?? DependencyService.Resolve<IAuthService>();
            GoToNextCommand = new Command(() => CurrentStep++, () => CurrentStep < MaxStep);
            GoToPrevCommand = new Command(() => CurrentStep--, () => CurrentStep > MinStep);
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>();
            int currStep = MinStep;
            RegistrationSteps.Add(new ChooseOperatorViewModel(currStep++, this.tagSettings, this.tagService, this.messageService));
            RegistrationSteps.Add(new ChooseAccountViewModel(currStep++, this.tagSettings, this.tagService, this.authService, this.messageService));
            RegistrationSteps.Add(new RegistrationStepViewModel(currStep++, this.tagSettings, this.tagService));
            RegistrationSteps.Add(new RegistrationStepViewModel(currStep++, this.tagSettings, this.tagService));
            RegistrationSteps.Add(new RegistrationStepViewModel(currStep++, this.tagSettings, this.tagService));

            RegistrationSteps.ForEach(x => x.StepCompleted += RegistrationStep_Completed);

            this.CurrentStep = DefaultStep;
        }

        private void RegistrationStep_Completed(object sender, EventArgs e)
        {
            RegistrationStepViewModel viewModel = (RegistrationStepViewModel)sender;
            switch (viewModel.Step)
            {
                case 2:
                    this.tagSettings.IncrementConfigurationStep();
                    GoToNextCommand.Execute();
                    break;

                case 3:
                    this.tagSettings.IncrementConfigurationStep();
                    GoToNextCommand.Execute();
                    break;

                case 4:
                    this.tagSettings.IncrementConfigurationStep();
                    GoToNextCommand.Execute();
                    break;

                case 5:
                    this.tagSettings.IncrementConfigurationStep();
                    GoToNextCommand.Execute();
                    break;

                default: // 0
                    this.tagSettings.SetDomain(((ChooseOperatorViewModel)viewModel).GetOperator(), string.Empty);
                    this.tagSettings.IncrementConfigurationStep();
                    GoToNextCommand.Execute();
                    break;
            }
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

        public override async Task RestoreState()
        {
            CurrentStep = await this.settingsService.RestoreState<int>(CurrentStepKey, DefaultStep);
        }

        public override async Task SaveState()
        {
            await this.settingsService.SaveState(CurrentStepKey, CurrentStep);
        }
    }
}