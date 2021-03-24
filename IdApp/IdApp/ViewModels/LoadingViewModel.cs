using IdApp.Extensions;
using IdApp.Views;
using IdApp.Views.Registration;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    /// <summary>
    /// The view model to bind to for a loading page, i.e. a page showing the loading/startup state of a Xamarin app.
    /// </summary>
    public class LoadingViewModel : NeuronViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="LoadingViewModel"/> class.
        /// </summary>
        public LoadingViewModel()
            : this(null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="LoadingViewModel"/> class.
        /// For unit tests.
        /// </summary>
        protected internal LoadingViewModel(
            INeuronService neuronService, 
            IUiDispatcher uiDispatcher,
            ITagProfile tagProfile, 
            INavigationService navigationService)
            : base(neuronService ?? DependencyService.Resolve<INeuronService>(), uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>())
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<ITagProfile>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
        }

        /// <inheritdoc />
        protected override async Task DoBind()
        {
            await base.DoBind();
            IsBusy = true;
            this.DisplayConnectionText = this.tagProfile.Step > RegistrationStep.Account;
            this.NeuronService.Loaded += NeuronService_Loaded;
        }

        /// <inheritdoc />
        protected override async Task DoUnbind()
        {
            this.NeuronService.Loaded -= NeuronService_Loaded;
            await base.DoUnbind();
        }

        #region Properties

        /// <summary>
        /// See <see cref="DisplayConnectionText"/>
        /// </summary>
        public static readonly BindableProperty DisplayConnectionTextProperty =
            BindableProperty.Create("DisplayConnectionText", typeof(bool), typeof(LoadingViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the user friendly connection text should be visible on screen or not.
        /// </summary>
        public bool DisplayConnectionText
        {
            get { return (bool)GetValue(DisplayConnectionTextProperty); }
            set { SetValue(DisplayConnectionTextProperty, value); }
        }

        #endregion

        /// <inheritdoc/>
        protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => SetConnectionStateAndText(e.State));
        }

        /// <inheritdoc/>
        protected override void SetConnectionStateAndText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(this.tagProfile);
            this.IsConnected = state == XmppState.Connected;
        }

        private void NeuronService_Loaded(object sender, LoadedEventArgs e)
        {
            if (e.IsLoaded)
            {
                this.IsBusy = false;

                this.UiDispatcher.BeginInvokeOnMainThread(async () =>
                {
                    if (this.tagProfile.IsComplete())
                    {
                        await this.navigationService.GoToAsync($"///{nameof(MainPage)}");
                    }
                    else
                    {
                        await this.navigationService.GoToAsync($"/{nameof(RegistrationPage)}");
                    }
                });
            }
        }
    }
}