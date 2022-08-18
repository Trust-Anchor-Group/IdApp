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
		/// <summary>
		/// Creates a new instance of the <see cref="MyContractsPage"/> class.
		/// </summary>
		public MyContractsPage()
		{
			MyContractsViewModel ViewModel = new();
			this.Title = ViewModel.Title;
			this.ViewModel = ViewModel;

			this.InitializeComponent();
		}

		private void ContractsSelectionChanged(object Sender, SelectionChangedEventArgs e)
		{
			if (this.ViewModel is MyContractsViewModel MyContractsViewModel)
			{
				object SelectedItem = this.Contracts.SelectedItem;

				if (SelectedItem is HeaderModel Category)
				{
					Category.Expanded = !Category.Expanded;
					MyContractsViewModel.AddOrRemoveContracts(Category, Category.Expanded);
				}
				else if (SelectedItem is ContractModel Contract)
				{
					MyContractsViewModel.ContractSelected(Contract.ContractId);

					if (Contract.HasEvents)
						MyContractsViewModel.NotificationService.DeleteEvents(Contract.Events);
				}
				else if (SelectedItem is EventModel Event)
				{
					MyContractsViewModel.UiSerializer.BeginInvokeOnMainThread(async () =>
					{
						try
						{
							await Event.Event.Open(MyContractsViewModel);

							await MyContractsViewModel.NotificationService.DeleteEvents(new NotificationEvent[] { Event.Event });
						}
						catch (Exception ex)
						{
							MyContractsViewModel.LogService.LogException(ex);
						}
					});
				}

				this.Contracts.SelectedItem = null;
			}
		}
	}
}
