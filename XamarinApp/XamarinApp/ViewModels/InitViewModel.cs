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

        public static readonly BindableProperty ProfileIsCompleteProperty =
            BindableProperty.Create("ProfileIsComplete", typeof(bool), typeof(InitViewModel), default(bool));

        public bool ProfileIsComplete
        {
            get { return (bool)GetValue(ProfileIsCompleteProperty); }
            set { SetValue(ProfileIsCompleteProperty, value); }
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            this.ProfileIsComplete = this.tagProfile.IsComplete();
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