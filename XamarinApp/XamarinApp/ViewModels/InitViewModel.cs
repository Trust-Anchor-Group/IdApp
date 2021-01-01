using System.Threading.Tasks;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.ViewModels;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using IDispatcher = Tag.Sdk.Core.IDispatcher;

namespace XamarinApp.ViewModels
{
    public class InitViewModel : BaseViewModel
    {
        private readonly TagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly IDispatcher dispatcher;

        public InitViewModel()
        {
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.dispatcher = DependencyService.Resolve<IDispatcher>();
            this.ConnectionStateText = AppResources.XmppState_Offline;
        }

        public static readonly BindableProperty ProfileIsCompleteProperty =
            BindableProperty.Create("ProfileIsComplete", typeof(bool), typeof(InitViewModel), default(bool));

        public bool ProfileIsComplete
        {
            get { return (bool)GetValue(ProfileIsCompleteProperty); }
            set { SetValue(ProfileIsCompleteProperty, value); }
        }

        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(InitViewModel), default(string));

        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.ProfileIsComplete = this.tagProfile.IsComplete();
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        protected override async Task DoUnbind()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.dispatcher.BeginInvokeOnMainThread(() => HandleConnectionStateChanged(e.State));
        }

        public void HandleConnectionStateChanged(XmppState state)
        {
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
        }
    }
}