using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

namespace IdApp.Pages.Main.SubscriptionRequest
{
    /// <summary>
    /// How to respond to a presence subscription request.
    /// </summary>
    public enum PresenceRequestAction
	{
        /// <summary>
        /// Accept request
        /// </summary>
        Accept,

        /// <summary>
        /// Reject request
        /// </summary>
        Reject,

        /// <summary>
        /// Ignore request.
        /// </summary>
        Ignore
	}

    /// <summary>
    /// Prompts the user for a response of a presence subscription request.
    /// </summary>
    public partial class SubscriptionRequestPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<PresenceRequestAction> result = new TaskCompletionSource<PresenceRequestAction>();
        private readonly string bareJid;

        /// <summary>
        /// Prompts the user for a response of a presence subscription request.
        /// </summary>
        /// <param name="BareJid">Bare JID of sender of request.</param>
        public SubscriptionRequestPopupPage(string BareJid)
        {
            this.bareJid = BareJid;

            InitializeComponent();
        }

        /// <summary>
        /// Bare JID of sender of request.
        /// </summary>
        public string BareJid => this.bareJid;

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

        private void Close()
        {
            this.OnIgnore(this, EventArgs.Empty);
        }

        private async void OnAccept(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(PresenceRequestAction.Accept);
        }

        private async void OnReject(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(PresenceRequestAction.Reject);
        }

        private async void OnIgnore(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(PresenceRequestAction.Ignore);
        }

        /// <summary>
        /// Task waiting for result. (null, null) means dialog was closed without providing a PIN.
        /// </summary>
        public Task<PresenceRequestAction> Result => this.result.Task;
    }
}