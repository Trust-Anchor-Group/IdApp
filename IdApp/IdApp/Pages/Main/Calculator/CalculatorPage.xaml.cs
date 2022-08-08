using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Main.Calculator
{
	/// <summary>
	/// A page that allows the user to calculate the value of a numerical input field.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CalculatorPage
	{
		/// <summary>
		/// A page that allows the user to calculate the value of a numerical input field.
		/// </summary>
		public CalculatorPage()
		{
			this.ViewModel = new CalculatorViewModel();

			this.InitializeComponent();
		}

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled</returns>
		protected override bool OnBackButtonPressed()
		{
			if (this.ViewModel is CalculatorViewModel CalculatorViewModel)
				CalculatorViewModel.EvaluateStack().Wait();

			this.ViewModel.NavigationService.GoBackAsync();
			return true;
		}
	}
}
