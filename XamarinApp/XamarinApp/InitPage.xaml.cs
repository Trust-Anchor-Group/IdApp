using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.ViewModels;

namespace XamarinApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InitPage
    {
        private readonly ITagService tagService;

        public InitPage()
            : this(new InitViewModel())
        {
        }

        protected internal InitPage(InitViewModel viewModel)
        {
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
            ViewModel = new InitViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);

            ProgressBar.IsRunning = true;
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
                    ProgressBar.IsRunning = false;
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                    await App.ShowPage();
                });
            }
        }
    }
}