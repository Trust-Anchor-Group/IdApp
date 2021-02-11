using IdApp.Extensions;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    /// <summary>
    /// A view model that holds Neuron state.
    /// </summary>
    public class NeuronViewModel : BaseViewModel
    {
        /// <summary>
        /// Creates an instance of a <see cref="NeuronViewModel"/>.
        /// </summary>
        /// <param name="neuronService"></param>
        /// <param name="uiDispatcher"></param>
        protected NeuronViewModel(INeuronService neuronService, IUiDispatcher uiDispatcher)
        {
            this.NeuronService = neuronService;
            this.UiDispatcher = uiDispatcher;
            this.ConnectionStateText = AppResources.XmppState_Offline;
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();
            this.SetConnectionStateAndText(this.NeuronService.State);
            this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        /// <inheritdoc/>
        protected override async Task DoUnbind()
        {
            this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        #region Properties

        /// <summary>
        /// Gets the current <see cref="IUiDispatcher"/>.
        /// </summary>
        protected IUiDispatcher UiDispatcher { get; }
        /// <summary>
        /// Gets the current <see cref="INeuronService"/>.
        /// </summary>
        protected INeuronService NeuronService { get; }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(NeuronViewModel), default(string));

        /// <summary>
        /// Gets the current connection state as a user friendly localized string.
        /// </summary>
        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(NeuronViewModel), default(bool));

        /// <summary>
        /// Gets whether the view model is connected to a Neuron server.
        /// </summary>
        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        #endregion

        /// <summary>
        /// Sets both the connection state and connection text to the appropriate value.
        /// </summary>
        /// <param name="state">The current state.</param>
        protected virtual void SetConnectionStateAndText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(null);
            this.IsConnected = state == XmppState.Connected;
        }

        /// <summary>
        /// Listens to connection state changes from the Neuron server.
        /// </summary>
        /// <param name="sender">The neuron service instance.</param>
        /// <param name="e">The event args.</param>
        protected virtual void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => this.SetConnectionStateAndText(e.State));
        }
    }
}