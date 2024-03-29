﻿using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.SellEDaler
{
	/// <summary>
	/// A page that allows the user to sell eDaler.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SellEDalerPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SellEDalerPage"/> class.
        /// </summary>
		public SellEDalerPage()
		{
            this.ViewModel = new SellEDalerViewModel();
			this.InitializeComponent();
        }
	}
}
