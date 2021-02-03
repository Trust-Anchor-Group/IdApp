using IdApp.Extensions;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    public class AppShellViewModel : BaseViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly INetworkService networkService;
        private readonly IUiDispatcher uiDispatcher;

        public AppShellViewModel()
        {
            this.tagProfile = DependencyService.Resolve<ITagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.networkService = DependencyService.Resolve<INetworkService>();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            this.ConnectionStateText = AppResources.XmppState_Offline;
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.IsOnline = this.networkService.IsOnline;
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
            this.networkService.ConnectivityChanged += NetworkService_ConnectivityChanged;
        }

        protected override Task DoUnbind()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            this.networkService.ConnectivityChanged -= NetworkService_ConnectivityChanged;
            return base.DoUnbind();
        }

        #region Properties

        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(MainViewModel), default(string));

        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(MainViewModel), default(bool));

        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        public static readonly BindableProperty IsOnlineProperty =
            BindableProperty.Create("IsOnline", typeof(bool), typeof(MainViewModel), default(bool));

        public bool IsOnline
        {
            get { return (bool)GetValue(IsOnlineProperty); }
            set { SetValue(IsOnlineProperty, value); }
        }

        #endregion

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.uiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.ConnectionStateText = e.State.ToDisplayText(this.tagProfile.Domain);
                this.IsConnected = e.State == XmppState.Connected;
            });
        }

        private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.uiDispatcher.BeginInvokeOnMainThread(() => this.IsOnline = this.networkService.IsOnline);
        }
    }
}