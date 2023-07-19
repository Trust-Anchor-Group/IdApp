using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Registration.Registration
{
    /// <summary>
    /// A page for guiding the user through the registration process for setting up a digital identity.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegistrationPage : ContentBasePage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="RegistrationPage"/> class.
		/// </summary>
		private RegistrationPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
        }

		/// <summary>
		/// Creates a new instance of the <see cref="RegistrationPage"/> class.
		/// </summary>
		public static async Task<RegistrationPage> Create()
		{
			RegistrationPage Result = new()
			{
				ViewModel = await RegistrationViewModel.Create()
			};

			Result.InitializeComponent();

			return Result;
		}
	}
}
