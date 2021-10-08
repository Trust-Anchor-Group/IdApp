using System;
using System.Collections.Generic;
using IdApp.Services.Navigation;
using IdApp.Services.UI;
using IdApp.Services.UI.QR;
using Xamarin.Forms.Xaml;
using ZXing;
using ZXing.Mobile;

namespace IdApp.Pages.Main.ScanQrCode
{
    /// <summary>
    /// A page to display for scanning of a QR code, either automatically via the camera, or by entering the code manually.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanQrCodePage
    {
        private readonly IUiSerializer uiSerializer;
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
            this.navigationService = App.Instantiate<INavigationService>();
            this.uiSerializer = App.Instantiate<IUiSerializer>();
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
                this.LinkEntry.Focus();
        }

        private void Scanner_OnScanResult(Result result)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                Scanner.IsAnalyzing = false; // Stop analysis until we navigate away so we don't keep reading qr codes
                string code = result.Text?.Trim();
                GetViewModel<ScanQrCodeViewModel>().Code = code;
                QrCode.TrySetResultAndClosePage(this.navigationService, this.uiSerializer, code);
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
                    await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnsupportedUriScheme, AppResources.Ok);
                    return;
                }
            }
            catch (Exception ex)
            {
                await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
                return;
            }

            QrCode.TrySetResultAndClosePage(this.navigationService, this.uiSerializer, code);
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns>Whether or not the back navigation was handled</returns>
        protected override bool OnBackButtonPressed()
        {
            QrCode.TrySetResultAndClosePage(this.navigationService, this.uiSerializer, string.Empty);
            return true;
        }

		private void ContentBasePage_SizeChanged(object sender, EventArgs e)
		{
            // cf. https://github.com/Redth/ZXing.Net.Mobile/issues/808

            this.Scanner.IsEnabled = false;
            this.Scanner.Options.CameraResolutionSelector = this.SelectLowestResolutionMatchingDisplayAspectRatio;
            this.Scanner.IsEnabled = true;
        }

        private CameraResolution SelectLowestResolutionMatchingDisplayAspectRatio(List<CameraResolution> AvailableResolutions)
        {
            CameraResolution Result = null;
            double AspectTolerance = 0.1;

            var DisplayOrientationHeight = this.Scanner.Width;
            var DisplayOrientationWidth = this.Scanner.Height;

            var TargetRatio = DisplayOrientationHeight / DisplayOrientationWidth;
            var TargetHeight = DisplayOrientationHeight;
            var MinDiff = double.MaxValue;

            foreach (CameraResolution Resolution in AvailableResolutions)
            {
                if (Math.Abs(((double)Resolution.Width / Resolution.Height) - TargetRatio) >= AspectTolerance)
                    continue;

                if (Math.Abs(Resolution.Height - TargetHeight) < MinDiff)
                {
                    MinDiff = Math.Abs(Resolution.Height - TargetHeight);
                    Result = Resolution;
                }
            }

            if (Result is null)
            {
                foreach (CameraResolution Resolution in AvailableResolutions)
                {
                    if (Math.Abs(Resolution.Height - TargetHeight) < MinDiff)
                    {
                        MinDiff = Math.Abs(Resolution.Height - TargetHeight);
                        Result = Resolution;
                    }
                }
            }

            return Result;
        }
    }
}