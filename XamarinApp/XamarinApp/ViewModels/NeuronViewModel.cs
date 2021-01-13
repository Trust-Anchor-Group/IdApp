using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.ViewModels;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using XamarinApp.Extensions;

namespace XamarinApp.ViewModels
{
    public class NeuronViewModel : BaseViewModel
    {
        protected NeuronViewModel(INeuronService neuronService, IUiDispatcher uiDispatcher)
        {
            this.NeuronService = neuronService;
            this.UiDispatcher = uiDispatcher;
            this.ConnectionStateText = AppResources.XmppState_Offline;
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.SetConnectionStateText(this.NeuronService.State);
            this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        protected override async Task DoUnbind()
        {
            this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        protected IUiDispatcher UiDispatcher { get; }
        protected INeuronService NeuronService { get; }

        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(NeuronViewModel), default(string));

        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(NeuronViewModel), default(bool));

        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        protected virtual void SetConnectionStateText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(null);
            this.IsConnected = state == XmppState.Connected;
        }

        public virtual void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => this.SetConnectionStateText(e.State));
        }

    }
}