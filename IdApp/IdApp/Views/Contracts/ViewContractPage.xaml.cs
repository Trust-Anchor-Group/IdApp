using System;
using IdApp.ViewModels.Contracts;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contracts
{
	/// <summary>
	/// A page that displays a specific contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates a new instance of the <see cref="ViewContractPage"/> class.
		/// </summary>
		public ViewContractPage()
		{
			this.navigationService = Types.Instantiate<INavigationService>(false);
			this.ViewModel = new ViewContractViewModel();
			InitializeComponent();
		}

		/// <inheritdoc/>
		protected override void OnDisappearing()
		{
			this.PhotoViewer.HidePhotos();
			base.OnDisappearing();
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

		private void Image_Tapped(object sender, EventArgs e)
		{
			Attachment[] attachments = this.GetViewModel<ViewContractViewModel>().Contract?.Attachments;
			this.PhotoViewer.ShowPhotos(attachments);
		}
	}
}