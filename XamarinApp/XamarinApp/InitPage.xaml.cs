using System;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
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
        private readonly IUiDispatcher uiDispatcher;

        public InitPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            ViewModel = new InitViewModel();
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);
            GetViewModel<InitViewModel>().IsBusy = true;
            this.neuronService.Loaded += NeuronService_Loaded;
        }

        protected override void OnDisappearing()
        {
            this.neuronService.Loaded -= NeuronService_Loaded;
            base.OnDisappearing();
        }

        private void NeuronService_Loaded(object sender, LoadedEventArgs e)
        {
            if (e.IsLoaded)
            {
                GetViewModel<InitViewModel>().IsBusy = false;

                this.uiDispatcher.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                    if (this.tagProfile.IsComplete())
                    {
                        await this.navigationService.ReplaceAsync("MainPage");
                    }
                    else
                    {
                        await this.navigationService.ReplaceAsync("RegistrationPage");
                    }
                });
            }
        }
    }
}