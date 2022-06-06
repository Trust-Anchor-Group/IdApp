using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IdApp.Extensions;
using IdApp.Services.Tag;
using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.Popups.Pin.ChangePin
{
	internal class ChangePinPopupViewModel : ObservableObject
	{
		private readonly ITagProfile tagProfile;

		private bool popupOpened = true;
		private bool incorrectPinAlertShown = false;

		private string oldPin = string.Empty;
		private bool oldPinFocused = true;

		private string newPin = string.Empty;
		private bool enteringNewPinStarted = false;

		private string retypedNewPin = string.Empty;
		private bool enteringRetypedNewPinStarted = false;

		public ChangePinPopupViewModel(ITagProfile tagProfile)
		{
			this.tagProfile = tagProfile ?? throw new ArgumentNullException(nameof(tagProfile));

			this.TryChangePinCommand = CommandFactory.Create(this.TryChangePin, () => this.CanTryChangePin);
			this.CloseCommand = CommandFactory.Create(this.Close);
		}

		public ICommand TryChangePinCommand { get; }

		public bool CanTryChangePin => this.NewPinStrength == PinStrength.Strong && this.NewPinMatchesRetypedNewPin;

		public ICommand CloseCommand { get; }

		public bool PopupOpened
		{
			get => this.popupOpened;
			set => this.SetProperty(ref this.popupOpened, value);
		}

		public bool IncorrectPinAlertShown
		{
			get => this.incorrectPinAlertShown;
			set => this.SetProperty(ref this.incorrectPinAlertShown, value);
		}

		public string OldPin
		{
			get => this.oldPin;
			set => this.SetProperty(ref this.oldPin, value);
		}

		public bool OldPinFocused
		{
			get => this.oldPinFocused;
			set => this.SetProperty(ref this.oldPinFocused, value);
		}

		public string NewPin
		{
			get => this.newPin;
			set => this.SetProperty(ref this.newPin, value);
		}

		public bool EnteringNewPinStarted
		{
			get => this.enteringNewPinStarted;
			set => this.SetProperty(ref this.enteringNewPinStarted, value);
		}

		public string RetypedNewPin
		{
			get => this.retypedNewPin;
			set => this.SetProperty(ref this.retypedNewPin, value);
		}

		public bool EnteringRetypedNewPinStarted
		{
			get => this.enteringRetypedNewPinStarted;
			set => this.SetProperty(ref this.enteringRetypedNewPinStarted, value);
		}

		public PinStrength NewPinStrength => this.tagProfile.ValidatePinStrength(this.NewPin);

		public bool NewPinMatchesRetypedNewPin => string.IsNullOrEmpty(this.NewPin) ? string.IsNullOrEmpty(this.RetypedNewPin) : this.NewPin.Equals(this.RetypedNewPin, StringComparison.Ordinal);

		protected override void OnPropertyChanged([CallerMemberName] string PropertyName = "")
		{
			base.OnPropertyChanged(PropertyName);

			if (PropertyName == nameof(this.NewPin))
			{
				// This somewhat complicated condition ensures that switching from null to an empty string does not count as EnteringNewPinStarted.
				if (!this.EnteringNewPinStarted && !string.IsNullOrEmpty(this.NewPin))
				{
					this.EnteringNewPinStarted = true;
				}

				this.OnPropertyChanged(nameof(this.NewPinStrength));
			}

			if (PropertyName == nameof(this.RetypedNewPin))
			{
				// This somewhat complicated condition ensures that switching from null to an empty string does not count as EnteringRetypedNewPinStarted.
				if (!this.EnteringRetypedNewPinStarted && !string.IsNullOrEmpty(this.RetypedNewPin))
				{
					this.EnteringRetypedNewPinStarted = true;
				}
			}

			if (PropertyName == nameof(this.NewPin) || PropertyName == nameof(this.RetypedNewPin))
			{
				this.OnPropertyChanged(nameof(this.NewPinMatchesRetypedNewPin));
				this.TryChangePinCommand.ChangeCanExecute();
			}

			if (PropertyName == nameof(this.IncorrectPinAlertShown) && !this.IncorrectPinAlertShown)
			{
				this.OldPin = string.Empty;
				this.OldPinFocused = true;
			}
		}

		private void TryChangePin()
		{
			if (this.tagProfile.ComputePinHash(this.OldPin) != this.tagProfile.PinHash)
			{
				this.IncorrectPinAlertShown = true;
			}
			else
			{
				this.PopupOpened = false;
			}
		}

		private void Close()
		{
			this.OldPin = null;

			this.EnteringNewPinStarted = false;
			this.NewPin = null;

			this.EnteringRetypedNewPinStarted = false;
			this.RetypedNewPin = null;

			this.PopupOpened = false;
		}
	}
}
