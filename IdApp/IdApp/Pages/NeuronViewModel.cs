using IdApp.Extensions;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;

namespace IdApp.Pages
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
        /// <param name="tagProfile"></param>
        protected NeuronViewModel(INeuronService neuronService, IUiDispatcher uiDispatcher, ITagProfile tagProfile)
        {
            this.NeuronService = neuronService ?? App.Instantiate<INeuronService>();
            this.UiDispatcher = uiDispatcher ?? App.Instantiate<IUiDispatcher>();
            this.TagProfile = tagProfile ?? App.Instantiate<ITagProfile>();
            this.StateSummaryText = AppResources.XmppState_Offline;
            this.ConnectionStateText = AppResources.XmppState_Offline;
            this.ConnectionStateColor = new SolidColorBrush(Color.Red);
            this.StateSummaryText = string.Empty;
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
        /// Gets the current <see cref="ITagProfile"/>.
        /// </summary>
        protected ITagProfile TagProfile { get; }

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
        public static readonly BindableProperty ConnectionStateColorProperty =
            BindableProperty.Create("ConnectionStateColor", typeof(Brush), typeof(NeuronViewModel), new SolidColorBrush(Color.Default));

        /// <summary>
        /// Gets the current connection state as a color.
        /// </summary>
        public Brush ConnectionStateColor
        {
            get { return (Brush)GetValue(ConnectionStateColorProperty); }
            set { SetValue(ConnectionStateColorProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty StateSummaryTextProperty =
            BindableProperty.Create("StateSummaryText", typeof(string), typeof(NeuronViewModel), default(string));

        /// <summary>
        /// Gets the current state summary as a user friendly localized string.
        /// </summary>
        public string StateSummaryText
        {
            get { return (string)GetValue(StateSummaryTextProperty); }
            set { SetValue(StateSummaryTextProperty, value); }
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
            this.ConnectionStateText = state.ToDisplayText();
            this.ConnectionStateColor = new SolidColorBrush(state.ToColor());
            this.IsConnected = state == XmppState.Connected;
			this.StateSummaryText = (this.TagProfile.LegalIdentity?.State)?.ToString() + " - " + this.ConnectionStateText;
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