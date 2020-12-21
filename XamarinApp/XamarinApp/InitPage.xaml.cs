using System;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using XamarinApp.Views;
using XamarinApp.Views.Registration;

namespace XamarinApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InitPage
    {
        private readonly INeuronService neuronService;
        private readonly TagProfile tagProfile;
        private readonly INavigationService navigationService;

        public InitPage()
            : this(new InitViewModel())
        {
            NavigationPage.SetHasNavigationBar(this, false);
            this.ViewModel = new InitViewModel();
        }

        protected internal InitPage(InitViewModel viewModel)
        {
            InitializeComponent();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            ViewModel = viewModel ?? new InitViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);

            this.neuronService.Loaded += NeuronService_Loaded;
        }

        protected override void OnDisappearing()
        {
            this.neuronService.Loaded -= NeuronService_Loaded;
            base.OnDisappearing();
        }

        private void NeuronService_Loaded(object sender, LoadedEventArgs e)
        {
            Dispatcher.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250));
                if (this.tagProfile.IsComplete() && this.tagProfile.LegalIdentity.State == IdentityState.Approved)
                {
                    await this.navigationService.ReplaceAsync(new MainPage());
                    return;
                }
                if (this.tagProfile.IsComplete() && this.tagProfile.LegalIdentity.State != IdentityState.Approved)
                {
                    IdentityState state = this.tagProfile.LegalIdentity.State;
                    switch (state)
                    {
                        case IdentityState.Compromised:
                            this.tagProfile.CompromizeLegalIdentity(this.tagProfile.LegalIdentity);
                            break;
                        case IdentityState.Obsoleted:
                        case IdentityState.Rejected:
                            this.tagProfile.RevokeLegalIdentity(this.tagProfile.LegalIdentity);
                            break;
                    }
                }
                await this.navigationService.ReplaceAsync(new RegistrationPage());
            });
        }
    }
}