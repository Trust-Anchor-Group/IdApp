﻿using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.AccountEvent
{
    /// <summary>
    /// A page that allows the user to view information about an account event.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AccountEventPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AccountEventPage"/> class.
        /// </summary>
		public AccountEventPage()
		{
            this.ViewModel = new AccountEventViewModel();
            
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
    }
}