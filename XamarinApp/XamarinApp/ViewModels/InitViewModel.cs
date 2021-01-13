using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.ViewModels;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using XamarinApp.Extensions;

namespace XamarinApp.ViewModels
{
    public class InitViewModel : BaseViewModel
    {
        private readonly ITagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly IUiDispatcher uiDispatcher;

        public InitViewModel()
        {
            this.tagProfile = DependencyService.Resolve<ITagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            this.ConnectionStateText = AppResources.XmppState_Offline;
        }

        public static readonly BindableProperty ProfileIsCompleteProperty =
            BindableProperty.Create("ProfileIsComplete", typeof(bool), typeof(InitViewModel), default(bool));

        public bool ProfileIsComplete
        {
            get { return (bool)GetValue(ProfileIsCompleteProperty); }
            set { SetValue(ProfileIsCompleteProperty, value); }
        }

        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(InitViewModel), default(string));

        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.ProfileIsComplete = this.tagProfile.IsComplete();
            this.SetConnectionStateText(this.neuronService.State);
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        protected override async Task DoUnbind()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.uiDispatcher.BeginInvokeOnMainThread(() => SetConnectionStateText(e.State));
        }

        private void SetConnectionStateText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(this.tagProfile.Domain);
        }
    }
}