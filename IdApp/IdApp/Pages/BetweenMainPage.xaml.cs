using Xamarin.Forms.Xaml;

namespace IdApp.Pages
{
	/// <summary>
	/// A page which is displayed when switching the application main page, while the old main page is performing cleanup asynchronously.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BetweenMainPage : ContentBasePage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BetweenMainPage"/> class.
		/// </summary>
		public BetweenMainPage()
		{
			this.InitializeComponent();
		}
	}
}
