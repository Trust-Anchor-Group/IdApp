using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Things.CanRead
{
	/// <summary>
	/// A page that asks the user if a remote entity is allowed to connect to a device.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CanReadPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="CanReadPage"/> class.
		/// </summary>
		public CanReadPage()
		{
			this.ViewModel = new CanReadModel();

			this.InitializeComponent();
		}
	}
}
