using System.Threading.Tasks;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.ViewModels;
using Xamarin.Forms;
using XamarinApp.Extensions;
using IDispatcher = Tag.Sdk.Core.IDispatcher;

namespace XamarinApp.ViewModels
{
    public class InitViewModel : BaseViewModel
    {
        private readonly TagProfile tagProfile;
        private readonly INeuronService neuronService;
        private readonly IDispatcher dispatcher;

        public InitViewModel()
        {
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.dispatcher = DependencyService.Resolve<IDispatcher>();
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
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        protected override async Task DoUnbind()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.dispatcher.BeginInvokeOnMainThread(() => e.State.ToDisplayText(this.tagProfile.Domain));
        }
    }
}