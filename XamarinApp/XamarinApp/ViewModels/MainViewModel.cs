using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly TagProfile tagProfile;
        private readonly INeuronService neuronService;

        public MainViewModel()
        {
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.ConnectionStateText = AppResources.XmppState_Offline;
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        protected override Task DoUnbind()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            return base.DoUnbind();
        }

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

        public void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Dispatcher.BeginInvokeOnMainThread(() => HandleConnectionStateChanged(e.State));
        }

        public void HandleConnectionStateChanged(XmppState state)
        {
            bool connected = false;

            switch (state)
            {
                case XmppState.Authenticating:
                    this.ConnectionStateText = AppResources.XmppState_Authenticating;
                    break;

                case XmppState.Binding:
                    this.ConnectionStateText = AppResources.XmppState_Binding;
                    break;

                case XmppState.Connected:
                    this.ConnectionStateText = string.Format(AppResources.XmppState_Connected, this.tagProfile.Domain);
                    connected = true;
                    break;

                case XmppState.Connecting:
                    this.ConnectionStateText = AppResources.XmppState_Connecting;
                    break;

                case XmppState.Error:
                    this.ConnectionStateText = AppResources.XmppState_Error;
                    break;

                case XmppState.FetchingRoster:
                    this.ConnectionStateText = AppResources.XmppState_FetchingRoster;
                    break;

                case XmppState.Registering:
                    this.ConnectionStateText = AppResources.XmppState_Registering;
                    break;

                case XmppState.RequestingSession:
                    this.ConnectionStateText = AppResources.XmppState_RequestingSession;
                    break;

                case XmppState.SettingPresence:
                    this.ConnectionStateText = AppResources.XmppState_SettingPresence;
                    break;

                case XmppState.StartingEncryption:
                    this.ConnectionStateText = AppResources.XmppState_StartingEncryption;
                    break;

                case XmppState.StreamNegotiation:
                    this.ConnectionStateText = AppResources.XmppState_StreamNegotiation;
                    break;

                case XmppState.StreamOpened:
                    this.ConnectionStateText = AppResources.XmppState_StreamOpened;
                    break;

                default:
                    this.ConnectionStateText = AppResources.XmppState_Offline;
                    break;
            }

            this.IsConnected = connected;
        }
    }
}