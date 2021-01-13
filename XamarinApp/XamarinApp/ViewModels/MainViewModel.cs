using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP;
using Xamarin.Essentials;
using Xamarin.Forms;
using XamarinApp.Extensions;

namespace XamarinApp.ViewModels
{
    public class MainViewModel : NeuronViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INetworkService networkService;

        public MainViewModel()
            : base(DependencyService.Resolve<INeuronService>(), DependencyService.Resolve<IUiDispatcher>())
        {
            this.tagProfile = DependencyService.Resolve<ITagProfile>();
            this.networkService = DependencyService.Resolve<INetworkService>();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.IsOnline = this.networkService.IsOnline;
            this.networkService.ConnectivityChanged += NetworkService_ConnectivityChanged;
        }

        protected override Task DoUnbind()
        {
            this.networkService.ConnectivityChanged -= NetworkService_ConnectivityChanged;
            return base.DoUnbind();
        }

        #region Properties

        public static readonly BindableProperty IsOnlineProperty =
            BindableProperty.Create("IsOnline", typeof(bool), typeof(MainViewModel), default(bool));

        public bool IsOnline
        {
            get { return (bool) GetValue(IsOnlineProperty); }
            set { SetValue(IsOnlineProperty, value); }
        }

        #endregion

        private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => this.IsOnline = this.networkService.IsOnline);
        }

        protected override void SetConnectionStateText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(this.tagProfile.Domain);
            this.IsConnected = state == XmppState.Connected;
        }
    }
}