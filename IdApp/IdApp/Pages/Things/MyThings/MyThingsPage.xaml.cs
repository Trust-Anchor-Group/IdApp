using IdApp.Services.Navigation;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Things.MyThings
{
	/// <summary>
	/// A page that displays a list of the current user's things.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyThingsPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates a new instance of the <see cref="MyThingsPage"/> class.
		/// </summary>
		public MyThingsPage()
		{
			this.navigationService = App.Instantiate<INavigationService>();
			this.ViewModel = new MyThingsViewModel();
			
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