﻿using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Things.IsFriend
{
	/// <summary>
	/// A page that asks the user if a remote entity is allowed to connect to a device.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class IsFriendPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="IsFriendPage"/> class.
		/// </summary>
		public IsFriendPage()
		{
			this.ViewModel = new IsFriendModel();

			this.InitializeComponent();
		}
	}
}
