namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationStepViewModel : BaseViewModel
    {
        public RegistrationStepViewModel(int step)
        {
            Step = step;
        }

        public int Step { get; }
    }
}