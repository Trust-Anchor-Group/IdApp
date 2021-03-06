﻿using IdApp.ViewModels.Things;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Things
{
	/// <summary>
	/// A page that displays a list of the current user's things.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyThingsPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates a new instance of the <see cref="MyThingsPage"/> class.
		/// </summary>
		public MyThingsPage()
		{
			this.navigationService = Types.Instantiate<INavigationService>(false);
			this.ViewModel = new MyThingsViewModel();
			
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