using IdApp.ViewModels.Contacts;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contacts
{
	/// <summary>
	/// A page that displays a list of the current user's contacts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContactsPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates a new instance of the <see cref="MyContactsPage"/> class.
		/// </summary>
		public MyContactsPage()
		{
			this.navigationService = DependencyService.Resolve<INavigationService>();
			this.ViewModel = new MyContactsViewModel();
			
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