using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

namespace IdApp.Popups.Xmpp.SubscribeTo
{
    /// <summary>
    /// Prompts the user for a response of a presence subscription request.
    /// </summary>
    public partial class SubscribeToPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<bool?> result = new();
        private readonly string bareJid;

        /// <summary>
        /// Prompts the user for a response of a presence subscription request.
        /// </summary>
        /// <param name="BareJid">Bare JID of sender of request.</param>
        public SubscribeToPopupPage(string BareJid)
        {
            this.bareJid = BareJid;

			this.InitializeComponent();

            this.BindingContext = this;
        }

        /// <summary>
        /// Bare JID of sender of request.
        /// </summary>
        public string BareJid => this.bareJid;

        private void OnCloseButtonTapped(object Sender, EventArgs e)
        {
			this.Close();
        }

        /// <inheritdoc/>
        protected override bool OnBackgroundClicked()
        {
			this.Close();
            return false;
        }

        private async void Close()
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(null);
        }

        private async void OnYes(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(true);
        }

        private async void OnNo(object Sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(false);
        }

        /// <summary>
        /// Task waiting for result.
        /// </summary>
        public Task<bool?> Result => this.result.Task;
    }
}
