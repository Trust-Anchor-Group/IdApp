using System;
using System.Threading.Tasks;
using IdApp.Services.Navigation;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.ViewContract
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
			this.navigationService = App.Instantiate<INavigationService>();
			this.ViewModel = new ViewContractViewModel();
			this.InitializeComponent();
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
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

		private void Image_Tapped(object sender, EventArgs e)
		{
			Attachment[] attachments = this.GetViewModel<ViewContractViewModel>().Contract?.Attachments;
			this.PhotoViewer.ShowPhotos(attachments);
		}
	}
}
