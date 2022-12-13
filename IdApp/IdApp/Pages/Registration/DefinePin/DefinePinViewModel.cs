using System;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using IdApp.Services.Xmpp;
using IdApp.Services.Tag;
using System.Runtime.CompilerServices;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Registration.DefinePin
{
	/// <summary>
	/// The view model to bind to when showing Step 5 of the registration flow: defining a PIN.
	/// </summary>
	public class DefinePinViewModel : RegistrationStepViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="DefinePinViewModel"/> class.
		/// </summary>
		public DefinePinViewModel()
			: base(RegistrationStep.Pin)
		{
			this.ContinueCommand = new Command(_ => this.Continue(), _ => this.CanContinue());
			this.SkipCommand = new Command(_ => this.Skip(), _ => this.CanSkip());
			this.Title = LocalizationResourceManager.Current["DefinePin"];
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.AssignProperties(this.XmppService.State);
			this.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			this.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;

			await base.OnDispose();
		}

		/// <inheritdoc />
		public override void ClearStepState()
		{
			this.Pin = string.Empty;
			this.EnteringPinStarted = false;

			this.RetypedPin = string.Empty;
			this.EnteringRetypedPinStarted = false;
		}

		#region Properties

		/// <summary>
		/// The command to bind to for continuing to the next step in the registration flow.
		/// </summary>
		public ICommand ContinueCommand { get; }
		/// <summary>
		/// The command to bind to for skipping this step and moving on to the next step in the registration flow.
		/// </summary>
		public ICommand SkipCommand { get; }

		/// <summary>
		/// The <see cref="Pin"/>
		/// </summary>
		public static readonly BindableProperty PinProperty =
			BindableProperty.Create(nameof(Pin), typeof(string), typeof(DefinePinViewModel), string.Empty);

		/// <summary>
		/// The PIn code entered by the user.
		/// </summary>
		public string Pin
		{
			get => (string)this.GetValue(PinProperty);
			set => this.SetValue(PinProperty, value);
		}

		/// <summary>
		/// The <see cref="RetypedPin"/>
		/// </summary>
		public static readonly BindableProperty RetypedPinProperty =
			BindableProperty.Create(nameof(RetypedPin), typeof(string), typeof(DefinePinViewModel), default(string));

		/// <summary>
		/// The retyped pin to use for validation against <see cref="Pin"/>.
		/// </summary>
		public string RetypedPin
		{
			get => (string)this.GetValue(RetypedPinProperty);
			set => this.SetValue(RetypedPinProperty, value);
		}

		/// <summary>
		/// The <see cref="EnteringPinStarted"/>
		/// </summary>
		public static readonly BindableProperty EnteringPinStartedProperty =
			BindableProperty.Create(nameof(EnteringPinStarted), typeof(bool), typeof(DefinePinViewModel), false);

		/// <summary>
		/// Gets or sets a value indicating if the user has started entering PIN.
		/// </summary>
		public bool EnteringPinStarted
		{
			get => (bool)this.GetValue(EnteringPinStartedProperty);
			set => this.SetValue(EnteringPinStartedProperty, value);
		}

		/// <summary>
		/// The <see cref="EnteringRetypedPinStarted"/>
		/// </summary>
		public static readonly BindableProperty EnteringRetypedPinStartedProperty =
			BindableProperty.Create(nameof(EnteringRetypedPinStarted), typeof(bool), typeof(DefinePinViewModel), false);

		/// <summary>
		/// Gets or sets a value indicating if the user has started entering retyped PIN.
		/// </summary>
		public bool EnteringRetypedPinStarted
		{
			get => (bool)this.GetValue(EnteringRetypedPinStartedProperty);
			set => this.SetValue(EnteringRetypedPinStartedProperty, value);
		}

		/// <summary>
		/// Gets the value indicating how strong the <see cref="Pin"/> is.
		/// </summary>
		public PinStrength PinStrength => this.TagProfile.ValidatePinStrength(this.Pin);

		/// <summary>
		/// Gets the value indicating whether the entered <see cref="Pin"/> is the same as the entered <see cref="RetypedPin"/>.
		/// </summary>
		public bool PinsMatch => string.IsNullOrEmpty(this.Pin) ? string.IsNullOrEmpty(this.RetypedPin) : this.Pin.Equals(this.RetypedPin, StringComparison.Ordinal);

		/// <summary>
		/// The <see cref="IsConnected"/>
		/// </summary>
		public static readonly BindableProperty IsConnectedProperty =
			BindableProperty.Create(nameof(IsConnected), typeof(bool), typeof(DefinePinViewModel), default(bool));

		/// <summary>
		/// Gets or sets if the app is connected to an XMPP server.
		/// </summary>
		public bool IsConnected
		{
			get => (bool)this.GetValue(IsConnectedProperty);
			set => this.SetValue(IsConnectedProperty, value);
		}

		/// <summary>
		/// The <see cref="ConnectionStateText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionStateTextProperty =
			BindableProperty.Create(nameof(ConnectionStateText), typeof(string), typeof(DefinePinViewModel), default(string));

		/// <summary>
		/// The user friendly connection state text to display to the user.
		/// </summary>
		public string ConnectionStateText
		{
			get => (string)this.GetValue(ConnectionStateTextProperty);
			set => this.SetValue(ConnectionStateTextProperty, value);
		}

		/// <summary>
		/// Gets a value indicating if the user already has an old pin set up.
		/// </summary>
		public bool OldPinExists => this.TagProfile.UsePin && !string.IsNullOrEmpty(this.TagProfile.PinHash);

		/// <summary>
		/// The <see cref="YouCanProtectYourWalletPinInfo"/>
		/// </summary>
		public static readonly BindableProperty YouCanProtectYourWalletPinInfoProperty =
			BindableProperty.Create(nameof(YouCanProtectYourWalletPinInfo), typeof(string), typeof(DefinePinViewModel), default(string));

		/// <summary>
		/// Enter PIN text to display to the user.
		/// </summary>
		public string YouCanProtectYourWalletPinInfo
		{
			get => (string)this.GetValue(YouCanProtectYourWalletPinInfoProperty);
			set => this.SetValue(YouCanProtectYourWalletPinInfoProperty, value);
		}

		#endregion

		/// <inheritdoc/>
		protected override void OnPropertyChanged([CallerMemberName] string PropertyName = null)
		{
			base.OnPropertyChanged(PropertyName);

			if (PropertyName == nameof(this.Pin))
			{
				this.EnteringPinStarted = true;
				this.OnPropertyChanged(nameof(this.PinStrength));
			}

			if (PropertyName == nameof(this.RetypedPin))
			{
				this.EnteringRetypedPinStarted = true;
			}

			if (PropertyName == nameof(this.Pin) || PropertyName == nameof(this.RetypedPin))
			{
				this.OnPropertyChanged(nameof(this.PinsMatch));
				this.ContinueCommand.ChangeCanExecute();
			}
		}

		private Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.AssignProperties(NewState);
			});

			return Task.CompletedTask;
		}

		private void AssignProperties(XmppState State)
		{
			this.SetConnectionStateAndText(State);
			this.ContinueCommand.ChangeCanExecute();
			this.SkipCommand.ChangeCanExecute();
		}

		private void SetConnectionStateAndText(XmppState State)
		{
			this.IsConnected = State == XmppState.Connected;
			this.ConnectionStateText = State.ToDisplayText();
			this.YouCanProtectYourWalletPinInfo = this.OldPinExists
				? LocalizationResourceManager.Current["YouCanProtectYourWalletPinInfoChange"] : LocalizationResourceManager.Current["YouCanProtectYourWalletPinInfo"];
		}

		private void Skip()
		{
			this.Complete(AddOrUpdatePin: false);
		}

		private bool CanSkip()
		{
			return this.XmppService.IsOnline;
		}

		private void Continue()
		{
			string PinToCheck = this.Pin ?? string.Empty;

			if (PinToCheck.Length < Constants.Authentication.MinPinLength)
			{
				this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["PinTooShort"], Constants.Authentication.MinPinLength));
				return;
			}
			if (PinToCheck.Trim() != PinToCheck)
			{
				this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PinMustNotIncludeWhitespace"]);
				return;
			}

			this.Complete(AddOrUpdatePin: true);
		}

		private void Complete(bool AddOrUpdatePin)
		{
			this.TagProfile.CompletePinStep(this.Pin, AddOrUpdatePin);

			this.OnStepCompleted(EventArgs.Empty);

			if (this.TagProfile.TestOtpTimestamp is not null)
				this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["WarningTitle"], LocalizationResourceManager.Current["TestOtpUsed"], LocalizationResourceManager.Current["Ok"]);
		}

		private bool CanContinue()
		{
			return this.PinStrength == PinStrength.Strong && this.PinsMatch && this.XmppService.IsOnline;
		}
	}
}
