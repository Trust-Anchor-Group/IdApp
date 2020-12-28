using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels;

namespace XamarinApp.Views
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
            this.navigationService.PopAsync();
            return true;
        }
    }
}