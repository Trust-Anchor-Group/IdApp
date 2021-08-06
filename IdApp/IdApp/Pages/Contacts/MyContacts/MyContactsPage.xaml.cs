﻿using IdApp.Pages.Contacts;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contacts.MyContacts
{
	/// <summary>
	/// A page that displays a list of the current user's contacts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContactsPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates a new instance of the <see cref="MyContactsPage"/> class.
		/// </summary>
		public MyContactsPage()
		{
			this.navigationService = App.Instantiate<INavigationService>();
			this.ViewModel = new ContactListViewModel();
			
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