using System;
using System.Threading.Tasks;
using IdApp.Pages.Registration.Registration;
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
    public partial class RegistrationPage : ContentBasePage
	{
        private readonly IUiSerializer uiSerializer;

        /// <summary>
        /// Creates a new instance of the <see cref="RegistrationPage"/> class.
        /// </summary>
        private RegistrationPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            this.uiSerializer = App.Instantiate<IUiSerializer>();
        }

		/// <summary>
		/// Creates a new instance of the <see cref="RegistrationPage"/> class.
		/// </summary>
		public static async Task<RegistrationPage> Create()
		{
			RegistrationPage Result = new();

			Result.ViewModel =  await RegistrationViewModel.Create();
			Result.InitializeComponent();

			return Result;
		}

		/// <summary>
		/// Overridden to sync the view with the view model when the page appears on screen.
		/// </summary>
		protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();

            if (Device.RuntimePlatform == Device.Android)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
				this.UpdateUiStep();
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
				if (this.ViewModel is RegistrationViewModel RegistrationViewModel)
				{
					int Step = RegistrationViewModel.CurrentStep;

					if (Step < (int)RegistrationStep.Complete)
					{
						int OtherStep = Step + ((Step > 0) ? -1 : 1);

						RegistrationViewModel.MuteStepSync();
						this.CarouselView.ScrollTo(OtherStep, position: ScrollToPosition.Center, animate: false);
						this.CarouselView.ScrollTo(Step, position: ScrollToPosition.Center, animate: false);
						this.uiSerializer.BeginInvokeOnMainThread(() => RegistrationViewModel.UnMuteStepSync());
					}
				}
            });
        }
    }
}
