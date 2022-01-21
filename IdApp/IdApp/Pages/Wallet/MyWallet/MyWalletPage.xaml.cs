﻿using Xamarin.Forms.Xaml;

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
            this.ViewModel = new MyWalletViewModel();

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

        /// <inheritdoc />
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = this.WalletTabBar.Show();
        }

        /// <inheritdoc />
        protected override async void OnDisappearing()
        {
            await this.WalletTabBar.Hide();
            base.OnDisappearing();
        }
    }
}