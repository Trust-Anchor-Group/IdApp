using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels.Registration;

namespace XamarinApp.Views.Registration
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegistrationPage
    {
        public RegistrationPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();
            ViewModel = new RegistrationViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Dispatcher.BeginInvokeOnMainThread(() =>
            {
                this.CarouselView.ScrollTo(GetViewModel<RegistrationViewModel>().CurrentStep, position: ScrollToPosition.Center);
            });
        }

        protected override bool OnBackButtonPressed()
        {
            RegistrationViewModel viewModel = GetViewModel<RegistrationViewModel>();
            if (viewModel.CanGoBack)
            {
                viewModel.GoToPrevCommand.Execute(null);
                return true;
            }
            return base.OnBackButtonPressed();
        }
    }
}