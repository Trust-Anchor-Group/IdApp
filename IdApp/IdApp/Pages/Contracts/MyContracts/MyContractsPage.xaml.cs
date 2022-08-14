using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Services.Navigation;
using IdApp.Services.Notification;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.MyContracts
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
		{
			this.navigationService = App.Instantiate<INavigationService>();

			MyContractsViewModel ViewModel = new();
			this.Title = ViewModel.Title;
			this.ViewModel = ViewModel;

			this.InitializeComponent();
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

		private void ContractsSelectionChanged(object Sender, SelectionChangedEventArgs e)
		{
			object SelectedItem = this.Contracts.SelectedItem;
			MyContractsViewModel ViewModel = this.GetViewModel<MyContractsViewModel>();

			if (SelectedItem is HeaderModel Category)
			{
				Category.Expanded = !Category.Expanded;
				ViewModel.AddOrRemoveContracts(Category, Category.Expanded);
			}
			else if (SelectedItem is ContractModel Contract)
			{
				ViewModel.ContractSelected(Contract.ContractId);

				if (Contract.HasEvents)
					ViewModel.NotificationService.DeleteEvents(Contract.Events);
			}
			else if (SelectedItem is EventModel Event)
			{
				ViewModel.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						await Event.Event.Open(ViewModel);

						await ViewModel.NotificationService.DeleteEvents(new NotificationEvent[] { Event.Event });
					}
					catch (Exception ex)
					{
						ViewModel.LogService.LogException(ex);
					}
				});
			}

			this.Contracts.SelectedItem = null;
		}
	}
}
