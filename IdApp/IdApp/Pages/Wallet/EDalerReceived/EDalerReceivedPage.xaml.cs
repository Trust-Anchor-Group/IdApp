﻿using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.EDalerReceived
{
    /// <summary>
    /// A page that displays information about eDaler received.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EDalerReceivedPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="EDalerReceivedPage"/> class.
        /// </summary>
		public EDalerReceivedPage()
		{
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new EDalerReceivedViewModel(
                App.Instantiate<ITagProfile>(),
                App.Instantiate<IUiSerializer>(),
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