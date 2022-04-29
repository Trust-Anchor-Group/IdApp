using IdApp.Services.UI;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IdApp.Pages
{
    /// <summary>
    /// A view model that holds the XMPP state.
    /// </summary>
    public class QrXmppViewModel : XmppViewModel
    {
        /// <summary>
        /// Creates an instance of a <see cref="XmppViewModel"/>.
        /// </summary>
        protected QrXmppViewModel()
            : base()
        {
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();
        }

        /// <inheritdoc/>
        protected override async Task DoUnbind()
        {
            await base.DoUnbind();
        }

		/// <summary>
		/// Generates a QR-code
		/// </summary>
		/// <param name="Uri">URI to encode in QR-code.</param>
		public void GenerateQrCode(string Uri)
		{
			byte[] Bin = Services.UI.QR.QrCode.GeneratePng(Uri, this.QrCodeWidth, this.QrCodeHeight);
			this.QrCode = ImageSource.FromStream(() => new MemoryStream(Bin));
			this.QrCodeBin = Bin;
			this.QrCodeContentType = Constants.MimeTypes.Png;
			this.HasQrCode = true;
		}

		/// <summary>
		/// Removes the QR-code
		/// </summary>
		public void RemoveQrCode()
		{
			this.QrCode = null;
			this.QrCodeBin = null;
			this.QrCodeContentType = string.Empty;
			this.HasQrCode = true;
		}

		#region Properties

		/// <summary>
		/// See <see cref="QrCodeProperty"/>
		/// </summary>
		public static readonly BindableProperty QrCodeProperty =
			BindableProperty.Create(nameof(QrCode), typeof(ImageSource), typeof(QrXmppViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				QrXmppViewModel viewModel = (QrXmppViewModel)b;
				viewModel.HasQrCode = !(newValue is null);
			});

		/// <summary>
		/// Generated QR code image for the identity
		/// </summary>
		public ImageSource QrCode
		{
			get => (ImageSource)this.GetValue(QrCodeProperty);
			set => this.SetValue(QrCodeProperty, value);
		}

		/// <summary>
		/// See <see cref="HasQrCode"/>
		/// </summary>
		public static readonly BindableProperty HasQrCodeProperty =
			BindableProperty.Create(nameof(HasQrCode), typeof(bool), typeof(QrXmppViewModel), default(bool));

		/// <summary>
		/// Determines whether there's a generated <see cref="QrCode"/> image for this identity.
		/// </summary>
		public bool HasQrCode
		{
			get => (bool)this.GetValue(HasQrCodeProperty);
			set => this.SetValue(HasQrCodeProperty, value);
		}

		/// <summary>
		/// See <see cref="QrCodeWidth"/>
		/// </summary>
		public static readonly BindableProperty QrCodeWidthProperty =
			BindableProperty.Create(nameof(QrCodeWidth), typeof(int), typeof(QrXmppViewModel), UiConstants.QrCode.DefaultImageWidth);

		/// <summary>
		/// Gets or sets the width, in pixels, of the QR Code image to generate.
		/// </summary>
		public int QrCodeWidth
		{
			get => (int)this.GetValue(QrCodeWidthProperty);
			set => this.SetValue(QrCodeWidthProperty, value);
		}

		/// <summary>
		/// See <see cref="QrCodeHeight"/>
		/// </summary>
		public static readonly BindableProperty QrCodeHeightProperty =
			BindableProperty.Create(nameof(QrCodeHeight), typeof(int), typeof(QrXmppViewModel), UiConstants.QrCode.DefaultImageHeight);

		/// <summary>
		/// Gets or sets the height, in pixels, of the QR Code image to generate.
		/// </summary>
		public int QrCodeHeight
		{
			get => (int)this.GetValue(QrCodeHeightProperty);
			set => this.SetValue(QrCodeHeightProperty, value);
		}

		/// <summary>
		/// See <see cref="QrCodeBin"/>
		/// </summary>
		public static readonly BindableProperty QrCodeBinProperty =
			BindableProperty.Create(nameof(QrCodeBin), typeof(byte[]), typeof(QrXmppViewModel), default(byte[]));

		/// <summary>
		/// Binary encoding of QR Code
		/// </summary>
		public byte[] QrCodeBin
		{
			get { return (byte[])this.GetValue(QrCodeBinProperty); }
			set => this.SetValue(QrCodeBinProperty, value);
		}

		/// <summary>
		/// See <see cref="QrCodeContentType"/>
		/// </summary>
		public static readonly BindableProperty QrCodeContentTypeProperty =
			BindableProperty.Create(nameof(QrCodeContentType), typeof(string), typeof(QrXmppViewModel), default(string));

		/// <summary>
		/// Content-Type of QR Code
		/// </summary>
		public string QrCodeContentType
		{
			get => (string)this.GetValue(QrCodeContentTypeProperty);
			set => this.SetValue(QrCodeContentTypeProperty, value);
		}

		#endregion
	}
}