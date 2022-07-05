using Xamarin.Forms.Xaml;

namespace IdApp.Pages
{
	/// <summary>
	/// A page which is displayed when an unexpected exception is encountered during the application startup.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BootstrapErrorPage : ContentBasePage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="BootstrapErrorPage"/> class.
		/// </summary>
		public BootstrapErrorPage()
		{
			this.InitializeComponent();
		}
	}
}
