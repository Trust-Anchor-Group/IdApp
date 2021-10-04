﻿using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Things.ViewClaimThing
{
    /// <summary>
    /// A page that displays a specific claim thing.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewClaimThingPage
	{
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="ViewClaimThingPage"/> class.
        /// </summary>
		public ViewClaimThingPage()
		{
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new ViewClaimThingViewModel(
                App.Instantiate<ITagProfile>(),
                App.Instantiate<IUiSerializer>(),
                App.Instantiate<INeuronService>(),
                this.navigationService ?? App.Instantiate<INavigationService>(),
                App.Instantiate<INetworkService>(),
                App.Instantiate<ILogService>());
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