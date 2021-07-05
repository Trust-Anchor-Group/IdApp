using IdApp.ViewModels;
using System;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
	/// <summary>
	/// A root, or main page, for the application. This is the starting point, from here you can navigate to other pages
	/// and take various actions.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MainPage"/> class.
		/// </summary>
		public MainPage()
		{
			InitializeComponent();
			ViewModel = new MainViewModel();
		}

		/// <inheritdoc />
		protected override void OnAppearing()
		{
			base.OnAppearing();
			_ = this.MainTabBar.Show();
		}

		/// <inheritdoc />
		protected override async void OnDisappearing()
		{
			await this.MainTabBar.Hide();
			base.OnDisappearing();
		}

		private void IdCard_Tapped(object sender, EventArgs e)
		{
			this.IdCard.Flip();
		}

		private void SharePhoto_Tapped(object sender, EventArgs e)
		{
			(this.ViewModel as MainViewModel)?.SharePhoto();
		}

		private void ShareQR_Tapped(object sender, EventArgs e)
		{
			(this.ViewModel as MainViewModel)?.ShareQR();
		}
	}
}