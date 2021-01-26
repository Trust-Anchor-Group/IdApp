using IdApp.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoadingPage
    {
        public LoadingPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            ViewModel = new LoadingViewModel();
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);
        }
    }
}