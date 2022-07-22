using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Things.ViewClaimThing
{
    /// <summary>
    /// A page that displays a specific claim thing.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewClaimThingPage
	{
        /// <summary>
        /// Creates a new instance of the <see cref="ViewClaimThingPage"/> class.
        /// </summary>
		public ViewClaimThingPage()
		{
            this.ViewModel = new ViewClaimThingViewModel();
			this.InitializeComponent();
		}

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns>Whether or not the back navigation was handled</returns>
        protected override bool OnBackButtonPressed()
        {
            this.ViewModel.NavigationService.GoBackAsync();
            return true;
        }
	}
}
