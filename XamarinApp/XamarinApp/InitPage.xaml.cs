using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using XamarinApp.Views.Registration;

namespace XamarinApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InitPage
    {
        private readonly ITagService tagService;
        private readonly INavigationService navigationService;

        public InitPage()
            : this(new InitViewModel())
        {
        }

        protected internal InitPage(InitViewModel viewModel)
        {
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            ViewModel = new InitViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);

            IsBusy = true;

            this.tagService.Loaded += TagService_Loaded;
        }

        protected override void OnDisappearing()
        {
            this.tagService.Loaded -= TagService_Loaded;
            base.OnDisappearing();
        }

        private void TagService_Loaded(object sender, LoadedEventArgs e)
        {
            if (e.IsLoaded)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    IsBusy = false;
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                    await this.navigationService.Set(new RegistrationPage());
                });
            }
        }
    }
}