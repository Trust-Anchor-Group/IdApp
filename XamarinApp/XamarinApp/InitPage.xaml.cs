using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels;

namespace XamarinApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InitPage : ContentPage
    {
        private readonly InitViewModel _viewModel;

        public InitPage()
            : this(new InitViewModel())
        {
        }

        protected internal InitPage(InitViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = new InitViewModel();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);

            ProgressBar.IsRunning = true;
            await _viewModel.Init();
            ProgressBar.IsRunning = false;

            await Task.Delay(TimeSpan.FromMilliseconds(250));

            await App.ShowPage();
        }

    }
}