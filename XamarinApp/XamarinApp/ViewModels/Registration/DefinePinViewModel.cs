using System;
using System.Windows.Input;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.Extensions;
using Xamarin.Forms;

namespace XamarinApp.ViewModels.Registration
{
    public class DefinePinViewModel : RegistrationStepViewModel
    {
        public DefinePinViewModel(
            TagProfile tagProfile,
            IUiDispatcher uiDispatcher,
            INeuronService neuronService,
            INavigationService navigationService,
            ISettingsService settingsService,
            ILogService logService)
            : base(RegistrationStep.Pin, tagProfile, uiDispatcher, neuronService, navigationService, settingsService, logService)
        {
            this.ContinueCommand = new Command(_ => Continue(), _ => CanContinue());
            this.SkipCommand = new Command(_ => Skip());
            this.Title = AppResources.DefinePin;
            this.PinIsTooShortMessage = string.Format(AppResources.PinTooShort, Constants.Authentication.MinPinLength);
        }

        public ICommand ContinueCommand { get; }
        public ICommand SkipCommand { get; }

        public static readonly BindableProperty PinProperty =
            BindableProperty.Create("Pin", typeof(string), typeof(DefinePinViewModel), string.Empty, propertyChanged: (b, oldValue, newValue) =>
        {
            DefinePinViewModel viewModel = (DefinePinViewModel)b;
            viewModel.UpdatePinState();
            viewModel.ContinueCommand.ChangeCanExecute();
        });

        public string Pin
        {
            get { return (string)GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        public static readonly BindableProperty RetypedPinProperty =
            BindableProperty.Create("RetypedPin", typeof(string), typeof(DefinePinViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                DefinePinViewModel viewModel = (DefinePinViewModel)b;
                viewModel.UpdatePinState();
                viewModel.ContinueCommand.ChangeCanExecute();
            });

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

        public static readonly BindableProperty PinsDoNotMatchProperty =
            BindableProperty.Create("PinsDoNotMatch", typeof(bool), typeof(DefinePinViewModel), default(bool));

        public bool PinsDoNotMatch
        {
            get { return (bool)GetValue(PinsDoNotMatchProperty); }
            set { SetValue(PinsDoNotMatchProperty, value); }
        }

        public static readonly BindableProperty PinIsTooShortProperty =
            BindableProperty.Create("PinIsTooShort", typeof(bool), typeof(DefinePinViewModel), default(bool));

        public bool PinIsTooShort
        {
            get { return (bool) GetValue(PinIsTooShortProperty); }
            set { SetValue(PinIsTooShortProperty, value); }
        }

        public static readonly BindableProperty PinIsTooShortMessageProperty =
            BindableProperty.Create("PinIsTooShortMessage", typeof(string), typeof(DefinePinViewModel), default(string));

        public string PinIsTooShortMessage
        {
            get { return (string) GetValue(PinIsTooShortMessageProperty); }
            set { SetValue(PinIsTooShortMessageProperty, value); }
        }

        public static readonly BindableProperty UsePinProperty =
            BindableProperty.Create("UsePin", typeof(bool), typeof(DefinePinViewModel), default(bool));

        public bool UsePin
        {
            get { return (bool)GetValue(UsePinProperty); }
            set { SetValue(UsePinProperty, value); }
        }

        private void Skip()
        {
            UsePin = false;
            this.TagProfile.SetPin(this.Pin, this.UsePin);
            OnStepCompleted(EventArgs.Empty);
        }

        private void Continue()
        {
            UsePin = true;

            string pinToCheck = this.Pin ?? string.Empty;

            if (pinToCheck.Length < Constants.Authentication.MinPinLength)
            {
                this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.PinTooShort, Constants.Authentication.MinPinLength));
                return;
            }
            if (pinToCheck.Trim() != pinToCheck)
            {
                this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinMustNotIncludeWhitespace);
                return;
            }

            this.TagProfile.SetPin(this.Pin, this.UsePin);

            OnStepCompleted(EventArgs.Empty);
        }

        private bool CanContinue()
        {
            return !PinsDoNotMatch;
        }
    }
}