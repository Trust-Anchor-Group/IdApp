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
    public class LoadingViewModel : NeuronViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INavigationService navigationService;

        public LoadingViewModel()
            : this(null, null, null, null)
        {
        }

        // For unit tests
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

        protected override async Task DoBind()
        {
            await base.DoBind();
            IsBusy = true;
            this.DisplayConnectionText = this.tagProfile.Step > RegistrationStep.Account;
            this.NeuronService.Loaded += NeuronService_Loaded;
        }

        protected override async Task DoUnbind()
        {
            this.NeuronService.Loaded -= NeuronService_Loaded;
            await base.DoUnbind();
        }

        #region Properties

        public static readonly BindableProperty DisplayConnectionTextProperty =
            BindableProperty.Create("DisplayConnectionText", typeof(bool), typeof(LoadingViewModel), default(bool));

        public bool DisplayConnectionText
        {
            get { return (bool)GetValue(DisplayConnectionTextProperty); }
            set { SetValue(DisplayConnectionTextProperty, value); }
        }

        #endregion

        protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => SetConnectionStateAndText(e.State));
        }

        protected override void SetConnectionStateAndText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(this.tagProfile.Domain);
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