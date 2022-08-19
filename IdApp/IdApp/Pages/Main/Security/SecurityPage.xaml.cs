using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Main.Security
{
	/// <summary>
	/// A page that allows the user to calculate the value of a numerical input field.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SecurityPage
	{
		/// <summary>
		/// A page that allows the user to calculate the value of a numerical input field.
		/// </summary>
		public SecurityPage()
		{
			this.ViewModel = new SecurityViewModel();

			this.InitializeComponent();
		}
	}
}
