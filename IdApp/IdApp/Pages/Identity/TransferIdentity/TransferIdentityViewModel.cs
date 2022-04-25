using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Resx;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Identity.TransferIdentity
{
	/// <summary>
	/// The view model to bind to for when displaying identities.
	/// </summary>
	public class TransferIdentityViewModel : QrXmppViewModel
	{
		private Timer timer;

		/// <summary>
		/// Creates an instance of the <see cref="TransferIdentityViewModel"/> class.
		/// </summary>
		public TransferIdentityViewModel()
			: base()
		{
			this.CopyUriToClipboard = new Command(async () => await this.CopyUriToClipboardClicked());
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out TransferIdentityNavigationArgs args))
				this.Uri = args.Uri;

			this.QrCodeWidth = 400;
			this.QrCodeHeight = 400;
			this.GenerateQrCode(this.Uri);

			this.timer = new Timer(this.Timeout, null, 60000, 60000);
		}

		private void Timeout(object P)
		{
			this.timer?.Dispose();
			this.timer = null;

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await this.NavigationService.GoBackAsync();
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			});
		}

		/// <inheritdoc/>
		protected override Task DoUnbind()
		{
			this.timer?.Dispose();
			this.timer = null;

			return base.DoUnbind();
		}

		#region Properties

		/// <summary>
		/// Command to copy URI to clipboard.
		/// </summary>
		public ICommand CopyUriToClipboard
		{
			get;
		}

		/// <summary>
		/// See <see cref="Uri"/>
		/// </summary>
		public static readonly BindableProperty UriProperty =
			BindableProperty.Create(nameof(Uri), typeof(string), typeof(TransferIdentityViewModel), default(string));

		/// <summary>
		/// Uri date of the identity
		/// </summary>
		public string Uri
		{
			get => (string)this.GetValue(UriProperty);
			set => this.SetValue(UriProperty, value);
		}

		#endregion

		private async Task CopyUriToClipboardClicked()
		{
			await Clipboard.SetTextAsync(this.Uri);
			await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.TagValueCopiedToClipboard);
		}
	}
}