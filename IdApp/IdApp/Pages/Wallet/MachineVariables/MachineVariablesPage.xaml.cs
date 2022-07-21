using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// A page that allows the user to view information about the current state of a state-machine.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MachineVariablesPage
	{
		/// <summary>
		/// A page that allows the user to view information about the current state of a state-machine.
		/// </summary>
		public MachineVariablesPage()
		{
			this.ViewModel = new MachineVariablesViewModel();

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
