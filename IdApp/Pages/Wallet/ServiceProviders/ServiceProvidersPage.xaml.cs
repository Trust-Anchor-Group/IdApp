using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// A page that allows the user to view its tokens.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ServiceProvidersPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersPage"/> class.
		/// </summary>
		public ServiceProvidersPage()
		{
			this.ViewModel = new ServiceProvidersViewModel();

			this.InitializeComponent();
		}
	}
}
