﻿using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.AccountEvent
{
    /// <summary>
    /// A page that allows the user to view information about an account event.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AccountEventPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="AccountEventPage"/> class.
        /// </summary>
		public AccountEventPage()
		{
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new AccountEventViewModel(
                App.Instantiate<ITagProfile>(),
                App.Instantiate<IUiDispatcher>(),
                App.Instantiate<INeuronService>(),
                this.navigationService);

            InitializeComponent();
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns>Whether or not the back navigation was handled</returns>
        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}