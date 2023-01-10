using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.BuyEDaler
{
    /// <summary>
    /// A page that displays information about eDaler received.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BuyEDalerPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="BuyEDalerPage"/> class.
        /// </summary>
		public BuyEDalerPage()
		{
            this.ViewModel = new BuyEDalerViewModel();
			this.InitializeComponent();
        }
	}
}
