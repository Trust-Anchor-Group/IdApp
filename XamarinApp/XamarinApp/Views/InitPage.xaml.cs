using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InitPage
    {
        public InitPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            ViewModel = new InitViewModel();
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);
        }
    }
}