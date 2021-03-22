using EDaler;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Navigation;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
using Xamarin.Forms;

namespace IdApp.ViewModels.Wallet
{
	/// <summary>
	/// The view model to bind to for when displaying thing claim information.
	/// </summary>
	public class EDalerUriViewModel : NeuronViewModel
	{
		private readonly ITagProfile tagProfile;
		private readonly ILogService logService;
		private readonly INavigationService navigationService;
		private readonly INetworkService networkService;

		/// <summary>
		/// Creates an instance of the <see cref="EDalerUriViewModel"/> class.
		/// </summary>
		public EDalerUriViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			INetworkService networkService,
			ILogService logService)
		: base(neuronService, uiDispatcher)
		{
			this.tagProfile = tagProfile;
			this.logService = logService;
			this.navigationService = navigationService;
			this.networkService = networkService;

			this.AcceptCommand = new Command(async _ => await Accept(), _ => IsConnected);

            this.ClickCommand = new Command(async x => await this.LabelClicked(x));
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out EDalerUriNavigationArgs args))
			{
				this.Uri = args.Uri.UriString;
			}

			AssignProperties();
			EvaluateAllCommands();

			this.tagProfile.Changed += TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.tagProfile.Changed -= TagProfile_Changed;
			await base.DoUnbind();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.AcceptCommand);
		}

		/// <inheritdoc/>
		protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
				this.EvaluateAllCommands();
			});
		}

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			this.UiDispatcher.BeginInvokeOnMainThread(AssignProperties);
		}

		#region Properties

		/// <summary>
		/// See <see cref="Uri"/>
		/// </summary>
		public static readonly BindableProperty UriProperty =
			BindableProperty.Create("Uri", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// iotdisco URI to process
		/// </summary>
		public string Uri
		{
			get { return (string)GetValue(UriProperty); }
			set { SetValue(UriProperty, value); }
		}

		/// <summary>
		/// The command to bind to for claiming a thing
		/// </summary>
		public ICommand AcceptCommand { get; }

		/// <summary>
		/// See <see cref="CanAccept"/>
		/// </summary>
		public static readonly BindableProperty CanAcceptProperty =
			BindableProperty.Create("CanAccept", typeof(bool), typeof(EDalerUriViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can claim a thing.
		/// </summary>
		public bool CanAccept
		{
			get { return this.NeuronService.State == XmppState.Connected; }
		}

		/// <summary>
		/// If PIN should be used.
		/// </summary>
		public bool UsePin => this.tagProfile?.UsePin ?? false;

		/// <summary>
		/// See <see cref="Pin"/>
		/// </summary>
		public static readonly BindableProperty PinProperty =
			BindableProperty.Create("Pin", typeof(string), typeof(EDalerUriViewModel), default(string));

		/// <summary>
		/// Gets or sets the PIN code for the identity.
		/// </summary>
		public string Pin
		{
			get { return (string)GetValue(PinProperty); }
			set { SetValue(PinProperty, value); }
		}

		#endregion

        /// <summary>
        /// Command to bind to for detecting when a tag value has been clicked on.
        /// </summary>
		public ICommand ClickCommand { get; }

		private Task LabelClicked(object _)
        {
			return Task.CompletedTask;	// TODO
		}

		private async Task Accept()
		{
			try
			{
				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
				{
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
					return;
				}

				(bool succeeded, Transaction Transaction) = await this.networkService.TryRequest(() => this.NeuronService.Wallet.SendUri(this.Uri));
				if (succeeded)
					await this.navigationService.GoBackAsync();
				else
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToProcessEDalerUri);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
		}
	}
}