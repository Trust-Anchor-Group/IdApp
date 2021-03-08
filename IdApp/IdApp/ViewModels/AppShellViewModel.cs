using System.Linq;
using IdApp.Extensions;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.ViewModels
{
    /// <summary>
    /// The view model to bind to for the App Shell, when using Xamarin Forms 5.0 or greater.
    /// This is the root, or bootstrap view model.
    /// </summary>
    public class AppShellViewModel : BaseViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly INetworkService networkService;
        private readonly IUiDispatcher uiDispatcher;

        /// <summary>
        /// Creates a new instance of the <see cref="AppShellViewModel"/> class.
        /// </summary>
        public AppShellViewModel()
            : this(null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="AppShellViewModel"/> class.
        /// For unit tests.
        /// </summary>
        protected internal AppShellViewModel(ITagProfile tagProfile, INeuronService neuronService, INetworkService networkService, IUiDispatcher uiDispatcher)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<ITagProfile>();
            this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            this.networkService = networkService ?? DependencyService.Resolve<INetworkService>();
            this.uiDispatcher = uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>();
            this.ConnectionStateText = AppResources.XmppState_Offline;
            this.IsOnline = this.networkService.IsOnline;
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
            this.networkService.ConnectivityChanged += NetworkService_ConnectivityChanged;
            this.UpdateLogInLogOutMenuItem();
        }

        #region Properties

        /// <summary>
        /// See <see cref="ConnectionStateText"/>
        /// </summary>
        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(MainViewModel), default(string));

        /// <summary>
        /// Gets or sets whether the connection state text, i.e a user friendly string showing XMPP connection info.
        /// </summary>
        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        /// <summary>
        /// See <see cref="IsConnected"/>
        /// </summary>
        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(MainViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the application is connected to an XMPP server.
        /// </summary>
        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        /// <summary>
        /// See <see cref="IsOnline"/>
        /// </summary>
        public static readonly BindableProperty IsOnlineProperty =
            BindableProperty.Create("IsOnline", typeof(bool), typeof(MainViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the application is online, i.e. has network access.
        /// </summary>
        public bool IsOnline
        {
            get { return (bool)GetValue(IsOnlineProperty); }
            set { SetValue(IsOnlineProperty, value); }
        }

        /// <summary>
        /// See <see cref="UserIsLoggedOut"/>
        /// </summary>
        public static readonly BindableProperty UserIsLoggedOutProperty =
            BindableProperty.Create("UserIsLoggedOut", typeof(bool), typeof(AppShellViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the user is logged in or out.
        /// </summary>
        public bool UserIsLoggedOut
        {
            get { return (bool)GetValue(UserIsLoggedOutProperty); }
            set { SetValue(UserIsLoggedOutProperty, value); }
        }

        /// <summary>
        /// See <see cref="LogInOutGlyph"/>
        /// </summary>
        public static readonly BindableProperty LogInOutGlyphProperty =
            BindableProperty.Create("LogInOutGlyph", typeof(FontImageSource), typeof(AppShellViewModel), default(FontImageSource));

        /// <summary>
        /// The icon to use for the log in/log out menu item.
        /// </summary>
        public FontImageSource LogInOutGlyph
        {
            get { return (FontImageSource) GetValue(LogInOutGlyphProperty); }
            set { SetValue(LogInOutGlyphProperty, value); }
        }

        /// <summary>
        /// See <see cref="LogInOutText"/>
        /// </summary>
        public static readonly BindableProperty LogInOutTextProperty =
            BindableProperty.Create("LogInOutText", typeof(string), typeof(AppShellViewModel), default(string));

        /// <summary>
        /// The text to use for the log in/log out menu item.
        /// </summary>
        public string LogInOutText
        {
            get { return (string) GetValue(LogInOutTextProperty); }
            set { SetValue(LogInOutTextProperty, value); }
        }

        #endregion

        private void UpdateLogInLogOutMenuItem()
        {
            if (this.UserIsLoggedOut)
            {
                this.LogInOutGlyph = new FontImageSource
                {
                    FontFamily = (OnPlatform<string>)Application.Current.Resources["FontAwesomeSolid"],
                    Glyph = SolidIcons.SignIn,
                    Color = (Color)Application.Current.Resources["HeadingBackground"]
                };
                this.LogInOutText = AppResources.SignIn;
            }
            else
            {
                this.LogInOutGlyph = new FontImageSource
                {
                    FontFamily = (OnPlatform<string>)Application.Current.Resources["FontAwesomeSolid"],
                    Glyph = SolidIcons.SignOut,
                    Color = (Color)Application.Current.Resources["HeadingBackground"]
                };
                this.LogInOutText = AppResources.SignOut;
            }
        }

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.uiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.ConnectionStateText = e.State.ToDisplayText(this.tagProfile.Domain);
                this.IsConnected = e.State == XmppState.Connected;
                this.UserIsLoggedOut = this.neuronService.IsLoggedOut;
                this.UpdateLogInLogOutMenuItem();
            });
        }

        private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.uiDispatcher.BeginInvokeOnMainThread(() => this.IsOnline = this.networkService.IsOnline);
        }
    }
}