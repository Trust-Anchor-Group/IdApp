﻿using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Things.ViewThing
{
	/// <summary>
	/// A page that displays information about a thing and allows the user to interact with it.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewThingPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ViewThingPage"/> class.
		/// </summary>
		public ViewThingPage()
		{
			this.ViewModel = new ViewThingModel();

			this.InitializeComponent();
		}
	}
}
