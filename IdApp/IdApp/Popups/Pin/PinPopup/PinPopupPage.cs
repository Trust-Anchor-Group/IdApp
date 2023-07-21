using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using IdApp.Services.Tag;

namespace IdApp.Popups.Pin.PinPopup
{
    /// <summary>
    /// Prompts the user for its PIN
    /// </summary>
    public partial class PinPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<string> result = new();

        /// <summary>
        /// Task waiting for result. null means dialog was closed without providing a PIN.
        /// </summary>
        public Task<string> Result => this.result.Task;

        /// <summary>
        /// Prompts the user for its PIN
        /// </summary>
        public PinPopupPage()
        {
            this.InitializeComponent();
            this.CloseWhenBackgroundIsClicked = false;
        }

		/// <inheritdoc/>
		protected override void LayoutChildren(double x, double y, double width, double height)
        {
            base.LayoutChildren(x, y, width, height + 1);
        }

        /// <inheritdoc/>
		protected override void OnAppearing()
        {
            base.OnAppearing();
            this.Pin.Focus();
        }

		/// <inheritdoc/>
		protected override bool OnBackButtonPressed()
		{
			return true;
		}

		private async void OnEnter(object Sender, EventArgs e)
        {
			string Pin = await App.CheckPinAndUnblockUser(this.Pin.Text, App.Instantiate<ITagProfile>());
			this.Pin.Text = "";
			this.result.TrySetResult(Pin);
		}

		private void Pin_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			this.EnterButton.IsEnabled = !string.IsNullOrEmpty(this.Pin.Text);
		}
	}
}
