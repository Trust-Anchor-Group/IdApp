using System;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Views;
using XamarinApp.Views.Registration;

namespace XamarinApp.ViewModels
{
    public class LoadingViewModel : NeuronViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INavigationService navigationService;
        /// <summary>
        /// Skip the first event when hooking up the event handler.
        /// </summary>
        private bool ignoreNeuronLoadedEvent;

        public LoadingViewModel()
        : base(DependencyService.Resolve<INeuronService>(), DependencyService.Resolve<IUiDispatcher>())
        {
            this.tagProfile = DependencyService.Resolve<ITagProfile>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            IsBusy = true;
            this.DisplayConnectionText = this.tagProfile.Step > RegistrationStep.Account;
            this.ignoreNeuronLoadedEvent = true;
            this.NeuronService.Loaded += NeuronService_Loaded;
            this.ignoreNeuronLoadedEvent = false;
        }

        protected override async Task DoUnbind()
        {
            this.NeuronService.Loaded -= NeuronService_Loaded;
            this.ignoreNeuronLoadedEvent = false;
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
            if (this.ignoreNeuronLoadedEvent)
                return;

            if (e.IsLoaded)
            {
                this.IsBusy = false;

                this.UiDispatcher.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
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
            else
            {
                
            }
        }
    }
}