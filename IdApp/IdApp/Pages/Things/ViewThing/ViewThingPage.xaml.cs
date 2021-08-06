using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Things.ViewThing
{
	/// <summary>
	/// A page that displays information about a thing and allows the user to interact with it.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewThingPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates a new instance of the <see cref="ViewThingPage"/> class.
		/// </summary>
		public ViewThingPage()
		{
			this.navigationService = App.Instantiate<INavigationService>();
			this.ViewModel = new ThingViewModel();
			
			InitializeComponent();
		}

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled</returns>
		protected override bool OnBackButtonPressed()
		{
			this.navigationService.GoBackAsync();
			return true;
		}
	}
}