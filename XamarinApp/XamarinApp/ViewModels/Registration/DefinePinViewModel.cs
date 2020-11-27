using System;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class DefinePinViewModel : RegistrationStepViewModel
    {
        public DefinePinViewModel(
            TagProfile tagProfile,
            INeuronService neuronService,
            INavigationService navigationService,
            ISettingsService settingsService)
            : base(RegistrationStep.Pin, tagProfile, neuronService, navigationService, settingsService)
        {
            this.PinChangedCommand = new Command<string>(s => Pin = s);
            this.RetypedPinChangedCommand = new Command<string>(s => RetypedPin = s);
            this.ContinueCommand = new Command(_ => Continue(), _ => CanContinue());
            this.SkipCommand = new Command(_ => Skip());
            this.Title = AppResources.DefinePin;
        }

        public ICommand PinChangedCommand { get; }
        public ICommand RetypedPinChangedCommand { get; }
        public ICommand ContinueCommand { get; }
        public ICommand SkipCommand { get; }

        public static readonly BindableProperty PinProperty =
            BindableProperty.Create("Pin", typeof(string), typeof(DefinePinViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
        {
            DefinePinViewModel viewModel = (DefinePinViewModel)b;
            viewModel.ContinueCommand.ChangeCanExecute();
            viewModel.UpdatePinState();
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
            PinsDoNotMatch = (Pin != RetypedPin);
        }

        public static readonly BindableProperty PinsDoNotMatchProperty =
            BindableProperty.Create("PinsDoNotMatch", typeof(bool), typeof(DefinePinViewModel), default(bool));

        public bool PinsDoNotMatch
        {
            get { return (bool)GetValue(PinsDoNotMatchProperty); }
            set { SetValue(PinsDoNotMatchProperty, value); }
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
            OnStepCompleted(EventArgs.Empty);
        }

        private void Continue()
        {
            UsePin = true;

            if (this.Pin.Length < 8)
            {
                this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.PinTooShort);
            }
            else if (this.Pin.Trim() != this.Pin)
            {
                this.NavigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.PinMustNotIncludeWhitespace);
            }

            OnStepCompleted(EventArgs.Empty);
        }

        private bool CanContinue()
        {
            return !PinsDoNotMatch;
        }
    }
}