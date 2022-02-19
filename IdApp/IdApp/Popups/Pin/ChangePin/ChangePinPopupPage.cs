using System;
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
        private readonly TaskCompletionSource<(string, string)> result = new TaskCompletionSource<(string, string)>();
        private readonly ITagProfile tagProfile;
        private readonly IUiSerializer uiSerializer;

        /// <summary>
        /// Prompts the user for its PIN
        /// </summary>
        public ChangePinPopupPage()
        {
            InitializeComponent();
            this.tagProfile = App.Instantiate<ITagProfile>();
            this.uiSerializer = App.Instantiate<IUiSerializer>();
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