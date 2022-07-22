using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

namespace IdApp.Popups.Tokens.AddTextNote
{
    /// <summary>
    /// Prompts the user for text to add as a note for a token.
    /// </summary>
    public partial class AddTextNotePage : PopupPage
    {
        private readonly TaskCompletionSource<bool?> result = new();

        /// <summary>
        /// Prompts the user for text to add as a note for a token.
        /// </summary>
        public AddTextNotePage()
        {
			this.InitializeComponent();

            this.BindingContext = this;
        }

        /// <summary>
        /// Text Note
        /// </summary>
        public string TextNote
        {
            get;
            set;
        }

        /// <summary>
        /// If note is personal or not.
        /// </summary>
        public bool Personal
		{
            get;
            set;
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

        private async void Close()
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(null);
        }

        private async void OnAdd(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(true);
        }

        private async void OnCancel(object sender, EventArgs e)
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
