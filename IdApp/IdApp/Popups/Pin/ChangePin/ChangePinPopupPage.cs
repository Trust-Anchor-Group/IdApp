using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IdApp.Resx;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

namespace IdApp.Popups.Pin.ChangePin
{
    /// <summary>
    /// Prompts the user for its PIN
    /// </summary>
    public partial class ChangePinPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<(string, string)> result = new();
        private readonly ITagProfile tagProfile;
        private readonly IUiSerializer uiSerializer;

        /// <summary>
        /// Prompts the user for its PIN
        /// </summary>
        public ChangePinPopupPage()
        {
            this.tagProfile = App.Instantiate<ITagProfile>();
            this.uiSerializer = App.Instantiate<IUiSerializer>();

			ChangePinPopupViewModel ViewModel = new(App.Instantiate<ITagProfile>());
			ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
			this.BindingContext = ViewModel;

			this.InitializeComponent();
		}

		private async void OnViewModelPropertyChanged(object Sender, PropertyChangedEventArgs EventArgs)
		{
			ChangePinPopupViewModel ViewModel = Sender as ChangePinPopupViewModel;

			if (EventArgs.PropertyName == nameof(ViewModel.PopupOpened) && !ViewModel.PopupOpened)
			{
				ViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
				await PopupNavigation.Instance.PopAsync();
				this.result.TrySetResult((ViewModel.OldPin, ViewModel.NewPin));
			}

			if (EventArgs.PropertyName == nameof(ViewModel.IncorrectPinAlertShown) && ViewModel.IncorrectPinAlertShown)
			{
				await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid, AppResources.Ok);
				ViewModel.OldPin = string.Empty;
				ViewModel.OldPinFocused = true;
			}
		}

		/// <inheritdoc/>
		protected override void OnAppearing()
		{
			base.OnAppearing();
            this.OldPin.Focus();
		}

        private void OldPinDone(object sender, EventArgs e)
        {
            this.NewPin.Focus();
        }

        private void NewPinDone(object sender, EventArgs e)
        {
            this.NewPin2.Focus();
        }

        private async void OnChange(object sender, EventArgs e)
        {
            string OldPin = this.OldPin.Text;
            string NewPin = this.NewPin.Text;

            if (this.tagProfile.ComputePinHash(OldPin) != this.tagProfile.PinHash)
            {
                await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid, AppResources.Ok);
                this.OldPin.Text = string.Empty;
                this.OldPin.Focus();
                return;
            }

            if (NewPin != this.NewPin2.Text)
            {
                await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PinsDoNotMatch, AppResources.Ok);
                this.NewPin.Text = string.Empty;
                this.NewPin2.Text = string.Empty;
                this.NewPin.Focus();
                return;
            }

            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult((OldPin, NewPin));
        }

        private void OnCloseButtonTapped(object sender, EventArgs e)
        {
            Close();
        }

        /// <inheritdoc/>
        protected override bool OnBackgroundClicked()
        {
            Close();
            return false;
        }

        private async void Close()
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult((null, null));
        }

        /// <summary>
        /// Task waiting for result. (null, null) means dialog was closed without providing a PIN.
        /// </summary>
        public Task<(string, string)> Result => this.result.Task;
    }
}
