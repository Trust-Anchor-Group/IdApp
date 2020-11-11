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
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await this.GetViewModel<RegistrationViewModel>().RestoreState();
        }

        protected override async void OnDisappearing()
        {
            await this.GetViewModel<RegistrationViewModel>().SaveState();
            base.OnDisappearing();
        }
    }
}