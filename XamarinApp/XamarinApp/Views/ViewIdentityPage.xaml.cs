using System.ComponentModel;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views
{
    [DesignTimeVisible(true)]
    public partial class ViewIdentityPage
    {
        private readonly INavigationService navigationService;

        public ViewIdentityPage()
            : this(null, null)
        {
        }

        public ViewIdentityPage(LegalIdentity identity)
            : this(identity, null)
        {
        }

        public ViewIdentityPage(LegalIdentity identity, SignaturePetitionEventArgs review)
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new ViewIdentityViewModel(
                identity,
                review,
                DependencyService.Resolve<TagProfile>(),
                DependencyService.Resolve<IUiDispatcher>(),
                DependencyService.Resolve<INeuronService>(),
                this.navigationService,
                DependencyService.Resolve<INetworkService>(),
                DependencyService.Resolve<ILogService>());
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
    }
}
