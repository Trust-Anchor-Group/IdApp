using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using XamarinApp.Extensions;

namespace XamarinApp.ViewModels
{
    public class InitViewModel : NeuronViewModel
    {
        private readonly ITagProfile tagProfile;

        public InitViewModel()
        : base(DependencyService.Resolve<INeuronService>(), DependencyService.Resolve<IUiDispatcher>())
        {
            this.tagProfile = DependencyService.Resolve<ITagProfile>();
        }

        public static readonly BindableProperty DisplayConnectionTextProperty =
            BindableProperty.Create("DisplayConnectionText", typeof(bool), typeof(InitViewModel), default(bool));

        public bool DisplayConnectionText
        {
            get { return (bool)GetValue(DisplayConnectionTextProperty); }
            set { SetValue(DisplayConnectionTextProperty, value); }
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.DisplayConnectionText = this.tagProfile.Step > RegistrationStep.Account;
        }

        protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiDispatcher.BeginInvokeOnMainThread(() => SetConnectionStateAndText(e.State));
        }

        protected override void SetConnectionStateAndText(XmppState state)
        {
            this.ConnectionStateText = state.ToDisplayText(this.tagProfile.Domain);
            this.IsConnected = state == XmppState.Connected;
        }
    }
}