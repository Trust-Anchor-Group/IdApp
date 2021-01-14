using System;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP;
using Xamarin.Essentials;
using Xamarin.Forms;
using XamarinApp.Extensions;

namespace XamarinApp.ViewModels
{
    public class MainViewModel : NeuronViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INetworkService networkService;

        public MainViewModel()
            : base(DependencyService.Resolve<INeuronService>(), DependencyService.Resolve<IUiDispatcher>())
        {
            this.tagProfile = DependencyService.Resolve<ITagProfile>();
            this.networkService = DependencyService.Resolve<INetworkService>();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.IsOnline = this.networkService.IsOnline;
            this.ContractsIsOnline = this.NeuronService.Contracts.IsOnline;
            this.NeuronService.Contracts.ConnectionStateChanged += Contracts_ConnectionStateChanged;
            this.networkService.ConnectivityChanged += NetworkService_ConnectivityChanged;
        }

        protected override Task DoUnbind()
        {
            this.NeuronService.Contracts.ConnectionStateChanged -= Contracts_ConnectionStateChanged;
            this.networkService.ConnectivityChanged -= NetworkService_ConnectivityChanged;
            return base.DoUnbind();
        }

        #region Properties

        public static readonly BindableProperty IsOnlineProperty =
            BindableProperty.Create("IsOnline", typeof(bool), typeof(MainViewModel), default(bool), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel model = (MainViewModel)b;
                model.NetworkStateText = model.IsOnline ? AppResources.Online : AppResources.Offline;
            });

        public bool IsOnline
        {
            get { return (bool) GetValue(IsOnlineProperty); }
            set { SetValue(IsOnlineProperty, value); }
        }

        public static readonly BindableProperty NetworkStateTextProperty =
            BindableProperty.Create("NetworkStateText", typeof(string), typeof(MainViewModel), default(string));

        public string NetworkStateText
        {
            get { return (string) GetValue(NetworkStateTextProperty); }
            set { SetValue(NetworkStateTextProperty, value); }
        }

        public static readonly BindableProperty ContractsIsOnlineProperty =
            BindableProperty.Create("ContractsIsOnline", typeof(bool), typeof(MainViewModel), default(bool), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel model = (MainViewModel)b;
                model.ContractsStateText = model.ContractsIsOnline ? AppResources.Online : AppResources.Offline;
            });

        public bool ContractsIsOnline
        {
            get { return (bool)GetValue(ContractsIsOnlineProperty); }
            set { SetValue(ContractsIsOnlineProperty, value); }
        }

        public static readonly BindableProperty ContractsStateTextProperty =
            BindableProperty.Create("ContractsStateText", typeof(string), typeof(MainViewModel), default(string));

        public string ContractsStateText
        {
            get { return (string) GetValue(ContractsStateTextProperty); }
            set { SetValue(ContractsStateTextProperty, value); }
        }

        public static readonly BindableProperty HasConnectionErrorsProperty =
            BindableProperty.Create("HasConnectionErrors", typeof(bool), typeof(MainViewModel), default(bool));

        public bool HasConnectionErrors
        {
            get { return (bool) GetValue(HasConnectionErrorsProperty); }
            set { SetValue(HasConnectionErrorsProperty, value); }
        }

        public static readonly BindableProperty ConnectionErrorsTextProperty =
            BindableProperty.Create("ConnectionErrorsText", typeof(string), typeof(MainViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                MainViewModel model = (MainViewModel)b;
                model.HasConnectionErrors = !string.IsNullOrWhiteSpace((string)newValue);
            });

        public string ConnectionErrorsText
        {
            get { return (string) GetValue(ConnectionErrorsTextProperty); }
            set { SetValue(ConnectionErrorsTextProperty, value); }
        }
        #endregion

        protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => this.SetConnectionStateAndText(e.State));
            this.ContractsIsOnline = this.NeuronService.IsOnline && this.NeuronService.Contracts.IsOnline;
        }

        private void Contracts_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.ContractsIsOnline = this.NeuronService.IsOnline && this.NeuronService.Contracts.IsOnline;
                SetConnectionErrorText();
            });
        }

        private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => this.IsOnline = this.networkService.IsOnline );
        }

        private void SetConnectionErrorText()
        {
            string latestError = this.NeuronService.LatestError;
            string latestConnectionError = this.NeuronService.LatestConnectionError;
            if (!string.IsNullOrWhiteSpace(latestError) && !string.IsNullOrWhiteSpace(latestConnectionError))
            {
                if (latestConnectionError != latestError)
                    this.ConnectionErrorsText = $"{latestConnectionError}{Environment.NewLine}{latestError}";
                else
                    this.ConnectionErrorsText = latestConnectionError;
            }
            else if (!string.IsNullOrWhiteSpace(latestConnectionError) && string.IsNullOrWhiteSpace(latestError))
            {
                this.ConnectionErrorsText = latestConnectionError;
            }
            else if (string.IsNullOrWhiteSpace(latestConnectionError) && !string.IsNullOrWhiteSpace(latestError))
            {
                this.ConnectionErrorsText = latestError;
            }
            else
            {
                this.ConnectionErrorsText = string.Empty;
            }
        }
        protected override void SetConnectionStateAndText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(this.tagProfile.Domain);
            this.IsConnected = state == XmppState.Connected;
            SetConnectionErrorText();
        }
    }
}