using System;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Settings;
using IdApp.Services.Tag;
using IdApp.Services.UI;

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
        /// <param name="tagProfile">The tag profile to work with.</param>
        /// <param name="uiSerializer">The UI dispatcher for alerts.</param>
        /// <param name="neuronService">The Neuron service for XMPP communication.</param>
        /// <param name="navigationService">The navigation service to use for app navigation</param>
        /// <param name="settingsService">The settings service for persisting UI state.</param>
        /// <param name="logService">The log service.</param>
        public DefinePinViewModel(
            ITagProfile tagProfile,
            IUiSerializer uiSerializer,
            INeuronService neuronService,
            INavigationService navigationService,
            ISettingsService settingsService,
            ILogService logService)
            : base(RegistrationStep.Pin, tagProfile, uiSerializer, neuronService, navigationService, settingsService, logService)
        {
            this.ContinueCommand = new Command(_ => Continue(), _ => CanContinue());
            this.SkipCommand = new Command(_ => Skip(), _ => CanSkip());
            this.Title = AppResources.DefinePin;
            this.PinIsTooShortMessage = string.Format(AppResources.PinTooShort, Constants.Authentication.MinPinLength);
        }

        /// <inheritdoc />
        protected override async Task DoBind()
        {
            await base.DoBind();
            AssignProperties(this.NeuronService.State);
            this.NeuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        /// <inheritdoc />
        protected override async Task DoUnbind()
        {
            this.NeuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            await base.DoUnbind();
        }

        /// <inheritdoc />
        public override void ClearStepState()
        {
            this.Pin = string.Empty;
            this.RetypedPin = string.Empty;
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
            BindableProperty.Create("Pin", typeof(string), typeof(DefinePinViewModel), string.Empty, propertyChanged: (b, oldValue, newValue) =>
            {
                DefinePinViewModel viewModel = (DefinePinViewModel)b;
                viewModel.UpdatePinState();
                viewModel.ContinueCommand.ChangeCanExecute();
            });

        /// <summary>
        /// The PIn code entered by the user.
        /// </summary>
        public string Pin
        {
            get { return (string)GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        /// <summary>
        /// The <see cref="RetypedPin"/>
        /// </summary>
        public static readonly BindableProperty RetypedPinProperty =
            BindableProperty.Create("RetypedPin", typeof(string), typeof(DefinePinViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                DefinePinViewModel viewModel = (DefinePinViewModel)b;
                viewModel.UpdatePinState();
                viewModel.ContinueCommand.ChangeCanExecute();
            });

        /// <summary>
        /// The retyped pin to use for validation against <see cref="Pin"/>.
        /// </summary>
        public string RetypedPin
        {
            get { return (string)GetValue(RetypedPinProperty); }
            set { SetValue(RetypedPinProperty, value); }
        }

        private void UpdatePinState()
        {
            PinsDoNotMatch = !string.IsNullOrWhiteSpace(Pin) && !string.IsNullOrWhiteSpace(RetypedPin) && (Pin != RetypedPin);
            PinIsTooShort = string.IsNullOrWhiteSpace(Pin) || Pin.Length < Constants.Authentication.MinPinLength;
        }

        /// <summary>
        /// The <see cref="PinsDoNotMatch"/>
        /// </summary>
        public static readonly BindableProperty PinsDoNotMatchProperty =
            BindableProperty.Create("PinsDoNotMatch", typeof(bool), typeof(DefinePinViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the entered Pin differ betweeen <see cref="Pin"/> and <see cref="RetypedPin"/>.
        /// </summary>
        public bool PinsDoNotMatch
        {
            get { return (bool)GetValue(PinsDoNotMatchProperty); }
            set { SetValue(PinsDoNotMatchProperty, value); }
        }

        /// <summary>
        /// The <see cref="PinIsTooShort"/>
        /// </summary>
        public static readonly BindableProperty PinIsTooShortProperty =
            BindableProperty.Create("PinIsTooShort", typeof(bool), typeof(DefinePinViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the entered PIN is too short.
        /// </summary>
        public bool PinIsTooShort
        {
            get { return (bool)GetValue(PinIsTooShortProperty); }
            set { SetValue(PinIsTooShortProperty, value); }
        }

        /// <summary>
        /// The <see cref="PinIsTooShortMessage"/>
        /// </summary>
        public static readonly BindableProperty PinIsTooShortMessageProperty =
            BindableProperty.Create("PinIsTooShortMessage", typeof(string), typeof(DefinePinViewModel), default(string));

        /// <summary>
        /// The localized message to display if and when the PIN code is too short.
        /// </summary>
        public string PinIsTooShortMessage
        {
            get { return (string)GetValue(PinIsTooShortMessageProperty); }
            set { SetValue(PinIsTooShortMessageProperty, value); }
        }

        /// <summary>
        /// The <see cref="UsePin"/>
        /// </summary>
        public static readonly BindableProperty UsePinProperty =
            BindableProperty.Create("UsePin", typeof(bool), typeof(DefinePinViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether a PIn should be used for validation.
        /// </summary>
        public bool UsePin
        {
            get { return (bool)GetValue(UsePinProperty); }
            set { SetValue(UsePinProperty, value); }
        }

        /// <summary>
        /// The <see cref="IsConnected"/>
        /// </summary>
        public static readonly BindableProperty IsConnectedProperty =
            BindableProperty.Create("IsConnected", typeof(bool), typeof(DefinePinViewModel), default(bool));

        /// <summary>
        /// Gets or sets if the app is connected to a Neuron server.
        /// </summary>
        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        /// <summary>
        /// The <see cref="ConnectionStateText"/>
        /// </summary>
        public static readonly BindableProperty ConnectionStateTextProperty =
            BindableProperty.Create("ConnectionStateText", typeof(string), typeof(DefinePinViewModel), default(string));

        /// <summary>
        /// The user friendly connection state text to display to the user.
        /// </summary>
        public string ConnectionStateText
        {
            get { return (string)GetValue(ConnectionStateTextProperty); }
            set { SetValue(ConnectionStateTextProperty, value); }
        }

        #endregion

        private void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            this.UiSerializer.BeginInvokeOnMainThread(() =>
            {
                AssignProperties(e.State);
            });
        }

        private void AssignProperties(XmppState state)
        {
            SetConnectionStateAndText(state);
            ContinueCommand.ChangeCanExecute();
            SkipCommand.ChangeCanExecute();
        }

        private void SetConnectionStateAndText(XmppState state)
        {
            IsConnected = state == XmppState.Connected;
            this.ConnectionStateText = state.ToDisplayText();
        }

        private void Skip()
        {
            UsePin = false;
            this.TagProfile.SetPin(this.Pin, this.UsePin);
            OnStepCompleted(EventArgs.Empty);
        }

        private bool CanSkip()
        {
            return this.NeuronService.IsOnline;
        }

        private void Continue()
        {
            UsePin = true;

            string pinToCheck = this.Pin ?? string.Empty;

            if (pinToCheck.Length < Constants.Authentication.MinPinLength)
            {
                this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.PinTooShort, Constants.Authentication.MinPinLength));
                return;
            }
            if (pinToCheck.Trim() != pinToCheck)
            {
                this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PinMustNotIncludeWhitespace);
                return;
            }

            this.TagProfile.SetPin(this.Pin, this.UsePin);

            OnStepCompleted(EventArgs.Empty);
        }

        private bool CanContinue()
        {
            return !PinsDoNotMatch && this.NeuronService.IsOnline;
        }
    }
}