using System;
using System.Threading.Tasks;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Registration.Registration
{
    /// <summary>
    /// A page for guiding the user through the registration process for setting up a digital identity.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegistrationPage
    {
        private readonly IUiSerializer uiSerializer;

        /// <summary>
        /// Creates a new instance of the <see cref="RegistrationPage"/> class.
        /// </summary>
        public RegistrationPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            this.uiSerializer = App.Instantiate<IUiSerializer>();
            ViewModel = new RegistrationViewModel();
            InitializeComponent();
        }

        /// <summary>
        /// Overridden to sync the view with the view model when the page appears on screen.
        /// </summary>
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
            this.uiSerializer.BeginInvokeOnMainThread(() =>
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
                    this.uiSerializer.BeginInvokeOnMainThread(() => vm.UnMuteStepSync());
                }
            });
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns>Whether or not the back navigation was handled</returns>
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