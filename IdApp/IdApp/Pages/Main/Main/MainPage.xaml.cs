using System;
using System.Threading.Tasks;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Main.Main
{
	/// <summary>
	/// A root, or main page, for the application. This is the starting point, from here you can navigate to other pages
	/// and take various actions.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainPage : ContentBasePage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MainPage"/> class.
		/// </summary>
		public MainPage()
		{
			this.InitializeComponent();
			this.ViewModel = new MainViewModel(this);
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

		private void IdCard_Tapped(object Sender, EventArgs e)
		{
			this.IdCard.Flip();
		}

		private void SharePhoto_Tapped(object Sender, EventArgs e)
		{
			(this.ViewModel as MainViewModel)?.SharePhoto();
		}

		private void ShareQR_Tapped(object Sender, EventArgs e)
		{
			(this.ViewModel as MainViewModel)?.ShareQR();
		}

		/// <summary>
		/// If the front view is showing.
		/// </summary>
		public bool IsFrontViewShowing => this.IdCard.IsFrontViewShowing;

		/// <summary>
		/// If the back view is showing.
		/// </summary>
		public bool IsBackViewShowing => this.IdCard.IsBackViewShowing;
	}
}
