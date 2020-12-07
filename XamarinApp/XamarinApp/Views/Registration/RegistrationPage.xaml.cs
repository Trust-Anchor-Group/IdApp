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
            GetViewModel<RegistrationViewModel>().StepChanged += RegistrationViewModel_StepChanged;
            UpdateUiStep();
        }

        protected override void OnDisappearing()
        {
            GetViewModel<RegistrationViewModel>().StepChanged -= RegistrationViewModel_StepChanged;
            base.OnDisappearing();
        }

        private void RegistrationViewModel_StepChanged(object sender, EventArgs e)
        {
            UpdateUiStep();
        }

        private void UpdateUiStep()
        {
            Dispatcher.BeginInvokeOnMainThread(() =>
            {
                int step = GetViewModel<RegistrationViewModel>().CurrentStep;
                this.CarouselView.ScrollTo(step, position: ScrollToPosition.Center);
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