using IdApp.Extensions;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using IdApp.Services.Xmpp;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages
{
    /// <summary>
    /// A view model that holds the XMPP state.
    /// </summary>
    public class XmppViewModel : BaseViewModel
    {
        /// <summary>
        /// Creates an instance of a <see cref="XmppViewModel"/>.
        /// </summary>
        protected XmppViewModel()
        {
            this.StateSummaryText = LocalizationResourceManager.Current["XmppState_Offline"];
            this.ConnectionStateText = LocalizationResourceManager.Current["XmppState_Offline"];
            this.ConnectionStateColor = new SolidColorBrush(Color.Red);
            this.StateSummaryText = string.Empty;
        }

        /// <inheritdoc/>
        protected override async Task OnInitialize()
        {
            await base.OnInitialize();

            this.SetConnectionStateAndText(this.XmppService.State);
            this.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
        }

        /// <inheritdoc/>
        protected override async Task OnDispose()
        {
            this.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
            await base.OnDispose();
        }

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create(nameof(ConnectionStateText), typeof(string), typeof(XmppViewModel), default(string));

        /// <summary>
        /// Gets the current connection state as a user friendly localized string.
        /// </summary>
        public string ConnectionStateText
        {
            get => (string)this.GetValue(ConnectionStateTextProperty);
            set => this.SetValue(ConnectionStateTextProperty, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty ConnectionStateColorProperty =
            BindableProperty.Create(nameof(ConnectionStateColor), typeof(Brush), typeof(XmppViewModel), new SolidColorBrush(Color.Default));

        /// <summary>
        /// Gets the current connection state as a color.
        /// </summary>
        public Brush ConnectionStateColor
        {
            get => (Brush)this.GetValue(ConnectionStateColorProperty);
            set => this.SetValue(ConnectionStateColorProperty, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty StateSummaryTextProperty =
            BindableProperty.Create(nameof(StateSummaryText), typeof(string), typeof(XmppViewModel), default(string));

        /// <summary>
        /// Gets the current state summary as a user friendly localized string.
        /// </summary>
        public string StateSummaryText
        {
            get => (string)this.GetValue(StateSummaryTextProperty);
            set => this.SetValue(StateSummaryTextProperty, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create(nameof(IsConnected), typeof(bool), typeof(XmppViewModel), default(bool));

        /// <summary>
        /// Gets whether the view model is connected to an XMPP server.
        /// </summary>
        public bool IsConnected
        {
            get => (bool)this.GetValue(IsConnectedProperty);
            set => this.SetValue(IsConnectedProperty, value);
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
        /// Listens to connection state changes from the XMPP server.
        /// </summary>
        /// <param name="Sender">The XMPP service instance.</param>
        /// <param name="e">The event args.</param>
        protected virtual void XmppService_ConnectionStateChanged(object Sender, ConnectionStateChangedEventArgs e)
        {
            this.UiSerializer.BeginInvokeOnMainThread(() => this.SetConnectionStateAndText(e.State));
        }
    }
}
