using System;
using System.Threading.Tasks;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.MyWallet
{
	/// <summary>
	/// A page that allows the user to view the contents of its wallet, pending payments and recent account events.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyWalletPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyWalletPage"/> class.
		/// </summary>
		public MyWalletPage()
		{
			this.ViewModel = new MyWalletViewModel(this);

			InitializeComponent();
		}

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled</returns>
		protected override bool OnBackButtonPressed()
		{
			this.ViewModel.NavigationService.GoBackAsync();
			return true;
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.WalletFrontTabBar.Show();
			await this.WalletBackTabBar.Show();
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected override async Task OnDisappearingAsync()
		{
			await this.WalletFrontTabBar.Hide();
			await this.WalletBackTabBar.Hide();
			await base.OnDisappearingAsync();
		}

		internal void WalletFlipView_Tapped(object sender, EventArgs e)
		{
			this.WalletFlipView.Flip();
		}

		private void WalletFlipView_BackViewShowing(object sender, EventArgs e)
		{
			if (this.ViewModel is MyWalletViewModel Model)
				Model.BindTokens();
		}
	}
}