using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Registration.ValidatePhoneNr
{
    /// <summary>
    /// A view to display the 'choose operator' during the registration process.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ValidatePhoneNrView
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ValidatePhoneNrView"/> class.
        /// </summary>
        public ValidatePhoneNrView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Scrolls down to the bottom of the view.
        /// </summary>
        public async void ScrollDown()
		{
            await this.ScrollView.ScrollToAsync(0, this.ScrollView.Height, true);
		}
	}
}