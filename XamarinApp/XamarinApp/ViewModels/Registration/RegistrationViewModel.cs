using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationViewModel : BaseViewModel
    {
        private static readonly string CurrentStepKey = $"{typeof(RegistrationViewModel).FullName}.CurrentStep";
        private const int MinStep = 0;
        private const int MaxStep = 4;
        private const int DefaultStep = MinStep;

        private readonly ISettingsService settingsService;

        public RegistrationViewModel()
            : this(null)
        {
        }

        // For unit tests
        protected internal RegistrationViewModel(ISettingsService settingsService)
        {
            this.settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            GoToNextCommand = new Command(() => CurrentStep++, () => CurrentStep < MaxStep);
            GoToPrevCommand = new Command(() => CurrentStep--, () => CurrentStep > MinStep);
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>();
            int currStep = MinStep;
            RegistrationSteps.Add(new ChooseOperatorViewModel(currStep++));
            RegistrationSteps.Add(new ChooseAccountViewModel(currStep++));
            RegistrationSteps.Add(new RegistrationStepViewModel(currStep++));
            RegistrationSteps.Add(new RegistrationStepViewModel(currStep++));
            RegistrationSteps.Add(new RegistrationStepViewModel(currStep++));
            this.CurrentStep = DefaultStep;
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

    public class RegistrationStepDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Step1Template { get; set; }
        public DataTemplate Step2Template { get; set; }
        public DataTemplate Step3Template { get; set; }
        public DataTemplate Step4Template { get; set; }
        public DataTemplate Step5Template { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            RegistrationStepViewModel viewModel = (RegistrationStepViewModel)item;

            switch (viewModel.Step)
            {
                case 1:
                    return Step2Template;
                case 2:
                    return Step3Template;
                case 3:
                    return Step4Template;
                case 4:
                    return Step5Template;
                default:
                    return Step1Template;
            }
        }
    }
}