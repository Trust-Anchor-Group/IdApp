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
	/// ChangePinPopupPage defines a popup which prompts the user for their PIN.
	/// </summary>
	public partial class ChangePinPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<(string, string)> result = new();
        private readonly IUiSerializer uiSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangePinPopupPage"/> class.
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
		/// A <see cref="Task"/> waiting for the result. <c>(null, null)</c> means the dialog was closed without providing a PIN.
		/// </summary>
		public Task<(string, string)> Result => this.result.Task;

		/// <inheritdoc/>
		protected override void OnAppearing()
		{
			base.OnAppearing();
			this.OldPinEntry.Focus();
		}

		/// <inheritdoc/>
		protected override bool OnBackgroundClicked()
        {
			this.ViewModel.CloseCommand.Execute(null);
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
			}
		}

		private void OnMainDisplayInfoChanged(object Sender, DisplayInfoChangedEventArgs EventArgs)
		{
			this.UpdateMainFrameWidth();
		}

		private void UpdateMainFrameWidth()
		{
			this.MainFrame.WidthRequest = 0.75 * DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
		}
	}
}
