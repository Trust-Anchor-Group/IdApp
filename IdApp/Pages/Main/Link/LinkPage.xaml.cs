using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Main.Link
{
	/// <summary>
	/// A page that allows the user to calculate the value of a numerical input field.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LinkPage
	{
		/// <summary>
		/// A page that allows the user to calculate the value of a numerical input field.
		/// </summary>
		public LinkPage()
		{
			this.ViewModel = new LinkViewModel();

			this.InitializeComponent();
		}
	}
}
