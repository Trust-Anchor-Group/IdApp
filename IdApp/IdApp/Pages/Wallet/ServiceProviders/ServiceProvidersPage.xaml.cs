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
			: this(null)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersPage"/> class.
		/// </summary>
		/// <param name="e">Navigation arguments.</param>
		public ServiceProvidersPage(ServiceProvidersNavigationArgs e)
		{
			this.ViewModel = new ServiceProvidersViewModel(e);

			this.InitializeComponent();
		}
	}
}
