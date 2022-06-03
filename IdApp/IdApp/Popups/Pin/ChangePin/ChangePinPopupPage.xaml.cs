using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IdApp.Resx;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Xamarin.Essentials;

namespace IdApp.Popups.Pin.ChangePin
{
    /// <summary>
    /// Prompts the user for its PIN
    /// </summary>
    public partial class ChangePinPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<(string, string)> result = new();
        private readonly IUiSerializer uiSerializer;

        /// <summary>
        /// Prompts the user for its PIN
        /// </summary>
        public ChangePinPopupPage()
		{
			this.uiSerializer = App.Instantiate<IUiSerializer>();

			ChangePinPopupViewModel ViewModel = new(App.Instantiate<ITagProfile>());
			ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
			this.BindingContext = ViewModel;

			this.InitializeComponent();

			this.UpdateMainFrameWidth();
			DeviceDisplay.MainDisplayInfoChanged += this.OnMainDisplayInfoChanged;
		}

		private ChangePinPopupViewModel ViewModel => this.BindingContext as ChangePinPopupViewModel;

		/// <summary>
		/// Task waiting for result. (null, null) means dialog was closed without providing a PIN.
		/// </summary>
		public Task<(string, string)> Result => this.result.Task;

        /// <inheritdoc/>
        protected override bool OnBackgroundClicked()
        {
			this.ViewModel.PopupOpened = false;
            return false;
		}

		private async void OnViewModelPropertyChanged(object Sender, PropertyChangedEventArgs EventArgs)
		{
			if (EventArgs.PropertyName == nameof(this.ViewModel.PopupOpened) && !this.ViewModel.PopupOpened)
			{
				this.ViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
				DeviceDisplay.MainDisplayInfoChanged -= this.OnMainDisplayInfoChanged;

				await PopupNavigation.Instance.PopAsync();
				this.result.TrySetResult((this.ViewModel.OldPin, this.ViewModel.NewPin));
			}

			if (EventArgs.PropertyName == nameof(this.ViewModel.IncorrectPinAlertShown) && this.ViewModel.IncorrectPinAlertShown)
			{
				await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid, AppResources.Ok);
				this.ViewModel.IncorrectPinAlertShown = false;
				this.ViewModel.OldPin = string.Empty;
				this.ViewModel.OldPinFocused = true;
			}
		}

		private void OnMainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
		{
			this.UpdateMainFrameWidth();
		}

		private void UpdateMainFrameWidth()
		{
			this.MainFrame.WidthRequest = 0.75 * DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
		}
	}
}
