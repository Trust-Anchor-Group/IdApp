﻿using IdApp.ViewModels.Things;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Things
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
            this.navigationService = Types.Instantiate<INavigationService>(false);
            this.ViewModel = new ViewClaimThingViewModel(
                Types.Instantiate<ITagProfile>(false),
                Types.Instantiate<IUiDispatcher>(false),
                Types.Instantiate<INeuronService>(false),
                this.navigationService ?? Types.Instantiate<INavigationService>(false),
                Types.Instantiate<INetworkService>(false),
                Types.Instantiate<ILogService>(false));
            InitializeComponent();
		}

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns></returns>
        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
	}
}