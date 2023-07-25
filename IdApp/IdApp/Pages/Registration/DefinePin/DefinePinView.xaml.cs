using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Registration.DefinePin
{
    /// <summary>
    /// A view to display the 'choose pin' during the registration process.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DefinePinView
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DefinePinView"/> class.
        /// </summary>
        public DefinePinView()
        {
			this.BindingContext = new DefinePinViewModel();

			this.InitializeComponent();
        }
    }
}
