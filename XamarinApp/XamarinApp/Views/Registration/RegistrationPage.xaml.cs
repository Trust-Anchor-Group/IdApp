using System;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels.Registration;

namespace XamarinApp.Views.Registration
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegistrationPage
    {
        private readonly IUiDispatcher uiDispatcher;

        public RegistrationPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            ViewModel = new RegistrationViewModel();
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Device.RuntimePlatform == Device.Android)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                UpdateUiStep();
            }
        }

        /// This is a hack. The issue is that the Carousel view doesn't reflect the CurrentStep binding correctly in the UI.
        /// The viewmodel is correct. The position property on the CarouselView is correct. But during restarts
        /// it still doesn't show the correct view template for that step. Instead it shows the last view template.
        /// So here we scroll back and forth one step to get it to be in sync with the viewmodel.
        private void UpdateUiStep()
        {
            this.uiDispatcher.BeginInvokeOnMainThread(() =>
            {
                RegistrationViewModel vm = GetViewModel<RegistrationViewModel>();
                int step = vm.CurrentStep;
                if (step < (int)RegistrationStep.Complete)
                {
                    int otherStep;
                    if (step > 0)
                    {
                        otherStep = step - 1;
                    }
                    else
                    {
                        otherStep = step + 1;
                    }

                    vm.MuteStepSync();
                    this.CarouselView.ScrollTo(otherStep, position: ScrollToPosition.Center, animate: false);
                    this.CarouselView.ScrollTo(step, position: ScrollToPosition.Center, animate: false);
                    this.uiDispatcher.BeginInvokeOnMainThread(() => vm.UnmuteStepSync());
                }
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