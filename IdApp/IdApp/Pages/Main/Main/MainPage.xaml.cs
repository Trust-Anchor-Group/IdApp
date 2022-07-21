using System;
using Xamarin.Forms.Xaml;
using Waher.Events;
using System.Threading.Tasks;

namespace IdApp.Pages.Main.Main
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
			this.InitializeComponent();
			this.ViewModel = new MainViewModel();
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.MainTabBar.Show();

			(this.ViewModel as MainViewModel)?.CheckOtpTimestamp();
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected override async Task OnDisappearingAsync()
		{
			await this.MainTabBar.Hide();
			await base.OnDisappearingAsync();
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
