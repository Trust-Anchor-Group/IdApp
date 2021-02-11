using IdApp.ViewModels;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    /// <summary>
    /// The page displaying current XMPP Communication
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class XmppCommunicationPage
    {
        private readonly INavigationService navigationService;
        /// <summary>
        /// Create an instance of a <see cref="XmppCommunicationPage"/>.
        /// </summary>
        public XmppCommunicationPage()
        {
            InitializeComponent();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new XmppCommunicationViewModel();
        }

        /// <inheritdoc/>
        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}