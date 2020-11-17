using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.ViewModels.Registration;

namespace XamarinApp.Views.Registration
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegistrationPage
    {
        public RegistrationPage()
        {
            InitializeComponent();
            ViewModel = new RegistrationViewModel(DependencyService.Resolve<TagServiceSettings>());
        }
    }
}