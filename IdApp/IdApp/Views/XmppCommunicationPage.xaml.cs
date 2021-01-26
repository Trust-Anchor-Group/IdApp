using IdApp.ViewModels;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class XmppCommunicationPage
    {
        private readonly INavigationService navigationService;

        public XmppCommunicationPage()
        {
            InitializeComponent();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new XmppCommunicationViewModel();
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}