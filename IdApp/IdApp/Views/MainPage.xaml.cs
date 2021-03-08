using IdApp.ViewModels;
using System;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    /// <summary>
    /// A root, or main page, for the application. This is the starting point, from here you can navigate to other pages
    /// and take various actions.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private readonly INeuronService neuronService;
        private bool logoutPanelIsShown;

        /// <summary>
        /// Creates a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.neuronService = DependencyService.Resolve<INeuronService>();
        }

        /// <inheritdoc />
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        /// <inheritdoc />
        protected override void OnDisappearing()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            base.OnDisappearing();
        }

        private async void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            const uint durationInMs = 300;
            if (e.IsUserInitiated)
            {
                if (this.neuronService.IsLoggedOut && e.State == XmppState.Offline)
                {
                    // Show (slide down) logout panel
                    await Task.Delay(TimeSpan.FromMilliseconds(durationInMs));
                    this.logoutPanelIsShown = true;
                    this.LogoutPanel.TranslationY = -Height;
                    await this.LogoutPanel.TranslateTo(0, 0, durationInMs, Easing.BounceOut);
                }
                else if (!this.neuronService.IsLoggedOut && this.logoutPanelIsShown)
                {
                    this.logoutPanelIsShown = false;
                    // Hide (slide up) logout panel
                    await Task.Delay(TimeSpan.FromMilliseconds(durationInMs));
                    await this.LogoutPanel.TranslateTo(0, -Height, durationInMs, Easing.SinOut);
                }
            }
            else if (logoutPanelIsShown)
            {
                this.logoutPanelIsShown = false;
                await this.LogoutPanel.TranslateTo(0, -Height, durationInMs, Easing.SinOut);
            }
        }

        private void IdCard_Tapped(object sender, EventArgs e)
        {
            this.IdCard.Flip();
        }
    }
}