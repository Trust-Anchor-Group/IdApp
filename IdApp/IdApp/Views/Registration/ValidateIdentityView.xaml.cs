using Xamarin.Forms.Xaml;

namespace IdApp.Views.Registration
{
    /// <summary>
    /// A view to display the 'validate identity' during the registration process.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ValidateIdentityView
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ValidateIdentityView"/> class.
        /// </summary>
        public ValidateIdentityView()
        {
            InitializeComponent();
        }
    }
}