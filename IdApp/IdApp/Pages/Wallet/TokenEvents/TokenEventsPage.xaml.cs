﻿using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.TokenEvents
{
	/// <summary>
	/// A page that allows the user to view information about a token.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenEventsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenEventsPage"/> class.
		/// </summary>
		public TokenEventsPage()
		{
			this.ViewModel = new TokenEventsViewModel();

			this.InitializeComponent();
		}

	}
}
