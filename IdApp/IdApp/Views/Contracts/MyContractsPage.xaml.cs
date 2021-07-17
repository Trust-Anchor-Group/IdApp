using System;
using IdApp.ViewModels.Contracts;
using IdApp.ViewModels.Contracts.ObjectModel;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
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
			this.navigationService = Types.Instantiate<INavigationService>(false);

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

		private void ToggleCategoryClicked(object Sender, EventArgs e)
		{
			if (e is TappedEventArgs e2 && e2.Parameter is ContractCategoryModel Category)
				Category.Expanded = !Category.Expanded;
		}
	}
}