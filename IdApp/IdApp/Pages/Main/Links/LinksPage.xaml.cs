using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Main.Links
{
	/// <summary>
	/// A page that allows the user to calculate the value of a numerical input field.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LinksPage
	{
		/// <summary>
		/// A page that allows the user to calculate the value of a numerical input field.
		/// </summary>
		public LinksPage()
		{
			this.ViewModel = new LinksViewModel();

			this.InitializeComponent();
		}
	}
}
