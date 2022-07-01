using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

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
        }

        /// <inheritdoc/>
		protected override void OnAppearing()
		{
			base.OnAppearing();
            this.Pin.Focus();
		}

		private async void OnEnter(object sender, EventArgs e)
        {
            this.result.TrySetResult(this.Pin.Text);
            await PopupNavigation.Instance.PopAsync();
        }

        private void OnCloseButtonTapped(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <inheritdoc/>
        protected override bool OnBackgroundClicked()
        {
			this.Close();
            return false;
        }

		/// <inheritdoc/>
		protected override bool OnBackButtonPressed()
        {
			this.Close();
            return false;
        }

        private async void Close()
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(null);
        }
    }
}
