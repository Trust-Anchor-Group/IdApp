using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Resx;
using IdApp.Services.UI;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Identity.TransferIdentity
{
	/// <summary>
	/// The view model to bind to for when displaying identities.
	/// </summary>
	public class TransferIdentityViewModel : XmppViewModel
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

			byte[] Data = Services.UI.QR.QrCode.GeneratePng(this.Uri, 400, 400);

			this.QrCode = ImageSource.FromStream(() => new MemoryStream(Data));
			this.QrCodeWidth = 400;
			this.QrCodeHeight = 400;

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
			get { return (string)this.GetValue(UriProperty); }
			set { this.SetValue(UriProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeProperty"/>
		/// </summary>
		public static readonly BindableProperty QrCodeProperty =
			BindableProperty.Create(nameof(QrCode), typeof(ImageSource), typeof(TransferIdentityViewModel), default(ImageSource));

		/// <summary>
		/// Generated QR code image for the identity
		/// </summary>
		public ImageSource QrCode
		{
			get { return (ImageSource)this.GetValue(QrCodeProperty); }
			set { this.SetValue(QrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeWidth"/>
		/// </summary>
		public static readonly BindableProperty QrCodeWidthProperty =
			BindableProperty.Create(nameof(QrCodeWidth), typeof(int), typeof(TransferIdentityViewModel), UiConstants.QrCode.DefaultImageWidth);

		/// <summary>
		/// Gets or sets the width, in pixels, of the QR Code image to generate.
		/// </summary>
		public int QrCodeWidth
		{
			get { return (int)this.GetValue(QrCodeWidthProperty); }
			set { this.SetValue(QrCodeWidthProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeHeight"/>
		/// </summary>
		public static readonly BindableProperty QrCodeHeightProperty =
			BindableProperty.Create(nameof(QrCodeHeight), typeof(int), typeof(TransferIdentityViewModel), UiConstants.QrCode.DefaultImageHeight);

		/// <summary>
		/// Gets or sets the height, in pixels, of the QR Code image to generate.
		/// </summary>
		public int QrCodeHeight
		{
			get { return (int)this.GetValue(QrCodeHeightProperty); }
			set { this.SetValue(QrCodeHeightProperty, value); }
		}

		#endregion

		private async Task CopyUriToClipboardClicked()
		{
			await Clipboard.SetTextAsync(this.Uri);
			await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.TagValueCopiedToClipboard);
		}
	}
}