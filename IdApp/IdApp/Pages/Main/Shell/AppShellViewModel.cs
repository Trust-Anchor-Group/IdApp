using System.Threading.Tasks;
using IdApp.Extensions;
using Waher.Networking.XMPP;
using Xamarin.Essentials;
using Xamarin.Forms;
using IdApp.Services.Xmpp;
using IdApp.Resx;

namespace IdApp.Pages.Main.Shell
{
	/// <summary>
	/// The view model to bind to for the App Shell, when using Xamarin Forms 5.0 or greater.
	/// This is the root, or bootstrap view model.
	/// </summary>
	public class AppShellViewModel : BaseViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="AppShellViewModel"/> class.
		/// </summary>
		protected internal AppShellViewModel()
		{
			this.ConnectionStateText = AppResources.XmppState_Offline;
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			await App.WaitForServiceSetup();

			this.IsOnline = this.NetworkService.IsOnline;

			this.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
			this.NetworkService.ConnectivityChanged += this.NetworkService_ConnectivityChanged;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
			this.NetworkService.ConnectivityChanged -= this.NetworkService_ConnectivityChanged;

			await base.DoUnbind();
		}

		#region Properties

		/// <summary>
		/// See <see cref="ConnectionStateText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionStateTextProperty =
			BindableProperty.Create(nameof(ConnectionStateText), typeof(string), typeof(Main.MainViewModel), default(string));

		/// <summary>
		/// Gets or sets whether the connection state text, i.e a user friendly string showing XMPP connection info.
		/// </summary>
		public string ConnectionStateText
		{
			get => (string)this.GetValue(ConnectionStateTextProperty);
			set => this.SetValue(ConnectionStateTextProperty, value);
		}

		/// <summary>
		/// See <see cref="IsConnected"/>
		/// </summary>
		public static readonly BindableProperty IsConnectedProperty =
			BindableProperty.Create(nameof(IsConnected), typeof(bool), typeof(Main.MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the application is connected to an XMPP server.
		/// </summary>
		public bool IsConnected
		{
			get => (bool)this.GetValue(IsConnectedProperty);
			set => this.SetValue(IsConnectedProperty, value);
		}

		/// <summary>
		/// See <see cref="IsOnline"/>
		/// </summary>
		public static readonly BindableProperty IsOnlineProperty =
			BindableProperty.Create(nameof(IsOnline), typeof(bool), typeof(Main.MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the application is online, i.e. has network access.
		/// </summary>
		public bool IsOnline
		{
			get => (bool)this.GetValue(IsOnlineProperty);
			set => this.SetValue(IsOnlineProperty, value);
		}

		#endregion

		private void XmppService_ConnectionStateChanged(object Sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.ConnectionStateText = e.State.ToDisplayText();
				this.IsConnected = e.State == XmppState.Connected;
			});
		}

		private void NetworkService_ConnectivityChanged(object Sender, ConnectivityChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() => this.IsOnline = this.NetworkService.IsOnline);
		}
	}
}
