using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.MyTokens
{
	/// <summary>
	/// A page that allows the user to view its tokens.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyTokensPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyTokensPage"/> class.
		/// </summary>
		public MyTokensPage()
		{
			this.ViewModel = new MyTokensViewModel();

			InitializeComponent();
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