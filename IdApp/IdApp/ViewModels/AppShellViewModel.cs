using System.Threading.Tasks;
using IdApp.Extensions;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.ViewModels
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

			this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
			this.NetworkService.ConnectivityChanged += NetworkService_ConnectivityChanged;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
			this.NetworkService.ConnectivityChanged -= NetworkService_ConnectivityChanged;

			await base.DoUnbind();
		}

		/// <summary>
		/// Current TAG Profile
		/// </summary>
		public ITagProfile TagProfile => Types.Instantiate<ITagProfile>(false);

		/// <summary>
		/// Current Neuron Service
		/// </summary>
		public INeuronService NeuronService => Types.Instantiate<INeuronService>(false);

		/// <summary>
		/// Current Network Service
		/// </summary>
		public INetworkService NetworkService => Types.Instantiate<INetworkService>(false);

		/// <summary>
		/// Current UI Dispatcher Service
		/// </summary>
		public IUiDispatcher UiDispatcher => Types.Instantiate<IUiDispatcher>(false);

		#region Properties

		/// <summary>
		/// See <see cref="ConnectionStateText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionStateTextProperty =
			BindableProperty.Create("ConnectionStateText", typeof(string), typeof(MainViewModel), default(string));

		/// <summary>
		/// Gets or sets whether the connection state text, i.e a user friendly string showing XMPP connection info.
		/// </summary>
		public string ConnectionStateText
		{
			get { return (string)GetValue(ConnectionStateTextProperty); }
			set { SetValue(ConnectionStateTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsConnected"/>
		/// </summary>
		public static readonly BindableProperty IsConnectedProperty =
			BindableProperty.Create("IsConnected", typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the application is connected to an XMPP server.
		/// </summary>
		public bool IsConnected
		{
			get { return (bool)GetValue(IsConnectedProperty); }
			set { SetValue(IsConnectedProperty, value); }
		}

		/// <summary>
		/// See <see cref="IsOnline"/>
		/// </summary>
		public static readonly BindableProperty IsOnlineProperty =
			BindableProperty.Create("IsOnline", typeof(bool), typeof(MainViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the application is online, i.e. has network access.
		/// </summary>
		public bool IsOnline
		{
			get { return (bool)GetValue(IsOnlineProperty); }
			set { SetValue(IsOnlineProperty, value); }
		}

		#endregion

		private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(() =>
			{
				this.ConnectionStateText = e.State.ToDisplayText(this.TagProfile);
				this.IsConnected = e.State == XmppState.Connected;
			});
		}

		private void NetworkService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(() => this.IsOnline = this.NetworkService.IsOnline);
		}
	}
}