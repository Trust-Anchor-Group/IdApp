using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.MachineReport
{
	/// <summary>
	/// A page that allows the user to view a state-machine report.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MachineReportPage
	{
		/// <summary>
		/// A page that allows the user to view a state-machine report.
		/// </summary>
		public MachineReportPage()
		{
			this.ViewModel = new MachineReportViewModel();

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
