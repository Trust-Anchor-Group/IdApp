using IdApp.ViewModels.Contracts;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contracts
{
	/// <summary>
	/// A page that displays a list of the current user's contracts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContractsPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates a new instance of the <see cref="MyContractsPage"/> class.
		/// </summary>
		public MyContractsPage()
			: this(ContractsListMode.MyContracts)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="MyContractsPage"/> class.
		/// </summary>
		/// <param name="ContractsListMode">What list of contracts to display.</param>
		protected MyContractsPage(ContractsListMode ContractsListMode)
		{
			this.navigationService = DependencyService.Resolve<INavigationService>();

			MyContractsViewModel ViewModel = new MyContractsViewModel(ContractsListMode);
			this.Title = ViewModel.Title;
			this.ViewModel = ViewModel;
			InitializeComponent();
		}

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns></returns>
		protected override bool OnBackButtonPressed()
		{
			this.navigationService.GoBackAsync();
			return true;
		}
	}
}