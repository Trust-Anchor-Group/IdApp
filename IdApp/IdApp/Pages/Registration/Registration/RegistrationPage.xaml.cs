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
		public RegistrationPage()
        {
			//NavigationPage.SetHasNavigationBar(this, false);

			RegistrationViewModel ViewModel = new();
			this.BindingContext = ViewModel;

			this.InitializeComponent();

			ViewModel.SetPagesContainer(this.PagesContainer.TabItems);
		}
	}
}
