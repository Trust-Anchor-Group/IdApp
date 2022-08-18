using IdApp.Services.Navigation;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.NewContract
{
    /// <summary>
    /// A page that allows the user to create a new contract.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage
	{
        /// <summary>
        /// Creates a new instance of the <see cref="NewContractPage"/> class.
        /// </summary>
		public NewContractPage()
        {
            this.ViewModel = new NewContractViewModel();
			this.InitializeComponent();
        }

        ///// <inheritdoc/>
        //protected override void OnAppearing()
        //{
        //    base.OnAppearing();
        //    this.ForceReRender(this.RootScrollView);
        //}
	}
}
