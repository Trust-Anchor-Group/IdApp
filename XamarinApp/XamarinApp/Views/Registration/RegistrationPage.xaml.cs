using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels.Registration;

namespace XamarinApp.Views.Registration
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegistrationPage
    {
        public RegistrationPage()
        {
            InitializeComponent();
            ViewModel = new RegistrationViewModel();
        }

        protected override bool OnBackButtonPressed()
        {
            RegistrationViewModel viewModel = GetViewModel<RegistrationViewModel>();
            if (viewModel.CanGoBack)
            {
                viewModel.GoToPrevCommand.Execute(null);
                return true;
            }
            return base.OnBackButtonPressed();
        }
    }
}