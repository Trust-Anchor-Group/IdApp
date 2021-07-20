using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Registration.ChooseAccount
{
    /// <summary>
    /// A view to display the 'create account or connect to existing account' during the registration process.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChooseAccountView
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ChooseAccountView"/> class.
        /// </summary>
        public ChooseAccountView()
        {
            InitializeComponent();
        }
    }
}