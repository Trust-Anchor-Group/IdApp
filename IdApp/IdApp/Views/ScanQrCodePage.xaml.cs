using IdApp.ViewModels;
using System;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;
using ZXing;

namespace IdApp.Views
{
    /// <summary>
    /// A page to display for scanning of a QR code, either automatically via the camera, or by entering the code manually.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanQrCodePage
    {
        private readonly IUiDispatcher uiDispatcher;
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="ScanQrCodePage"/> class.
        /// </summary>
        public ScanQrCodePage()
            : this(null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ScanQrCodePage"/> class.
        /// For unit tests.
        /// </summary>
        /// <param name="viewModel">The view model to use.</param>
        protected internal ScanQrCodePage(ScanQrCodeViewModel viewModel)
        {
            this.ViewModel = viewModel ?? new ScanQrCodeViewModel();
            this.navigationService = Types.Instantiate<INavigationService>(false);
            this.uiDispatcher = Types.Instantiate<IUiDispatcher>(false);
            InitializeComponent();
        }

        /// <summary>
        /// Overridden to initialize the QR Code scanner when the page appears on screen.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            GetViewModel<ScanQrCodeViewModel>().ModeChanged += ViewModel_ModeChanged;
            Scanner.IsScanning = true;
            Scanner.IsAnalyzing = true;
        }

        /// <summary>
        /// Overridden to un-initialize the QR Code scanner when the page disappears from screen.
        /// </summary>
        protected override void OnDisappearing()
        {
            Scanner.IsAnalyzing = false;
            Scanner.IsScanning = false;
            GetViewModel<ScanQrCodeViewModel>().ModeChanged -= ViewModel_ModeChanged;
            base.OnDisappearing();
        }

        private void ViewModel_ModeChanged(object sender, EventArgs e)
        {
            if (GetViewModel<ScanQrCodeViewModel>().ScanIsManual)
            {
                this.LinkEntry.Focus();
            }
        }

        private void Scanner_OnScanResult(Result result)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                Scanner.IsAnalyzing = false; // Stop analysis until we navigate away so we don't keep reading qr codes
                string code = result.Text?.Trim();
                GetViewModel<ScanQrCodeViewModel>().Code = code;
                QrCode.TrySetResultAndClosePage(this.navigationService, this.uiDispatcher, code);
            }
        }

        private async void OpenButton_Click(object sender, EventArgs e)
        {
            string code = GetViewModel<ScanQrCodeViewModel>().LinkText?.Trim();
            try
            {
                string scheme = Constants.UriSchemes.GetScheme(code);

                if (string.IsNullOrWhiteSpace(scheme))
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.UnsupportedUriScheme, AppResources.Ok);
                    return;
                }
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
                return;
            }

            QrCode.TrySetResultAndClosePage(this.navigationService, this.uiDispatcher, code);
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns></returns>
        protected override bool OnBackButtonPressed()
        {
            QrCode.TrySetResultAndClosePage(this.navigationService, this.uiDispatcher, string.Empty);
            return true;
        }
    }
}