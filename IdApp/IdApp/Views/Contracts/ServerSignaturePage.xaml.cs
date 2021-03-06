﻿using IdApp.ViewModels.Contracts;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A page that displays a server signature.
    /// </summary>
    [DesignTimeVisible(true)]
	public partial class ServerSignaturePage
	{
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="ServerSignaturePage"/> class.
        /// </summary>
		public ServerSignaturePage()
		{
            this.navigationService = Types.Instantiate<INavigationService>(false);
            ViewModel = new ServerSignatureViewModel();
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
