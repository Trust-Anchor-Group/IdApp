using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationViewModel : BaseViewModel
    {
        private static readonly string CurrentStepKey = $"{typeof(RegistrationViewModel).FullName}.CurrentStep";
        private readonly ISettingsService settingsService;

        public RegistrationViewModel()
            : this(null)
        {
        }

        // For unit tests
        protected internal RegistrationViewModel(ISettingsService settingsService)
        {
            this.settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
        }

        public static readonly BindableProperty CurrentStepProperty =
            BindableProperty.Create("CurrentStep", typeof(int), typeof(RegistrationViewModel), default(int));

        public int CurrentStep
        {
            get { return (int)GetValue(CurrentStepProperty); }
            set { SetValue(CurrentStepProperty, value); }
        }

        public async Task RestoreState()
        {
            CurrentStep = await this.settingsService.RestoreState<int>(CurrentStepKey);
        }

        public async Task SaveState()
        {
            await this.settingsService.SaveState(CurrentStepKey, CurrentStep);
        }
    }
}