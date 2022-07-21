using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdApp.Resx;
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
		/// <summary>
		/// Creates a new instance of the <see cref="ScanQrCodePage"/> class.
		/// </summary>
		public ScanQrCodePage()
		{
			this.ViewModel = new ScanQrCodeViewModel();
			this.InitializeComponent();

			this.Scanner.Options = new MobileBarcodeScanningOptions
			{
				PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE },
				TryHarder = true
			};
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			this.GetViewModel<ScanQrCodeViewModel>().ModeChanged += this.ViewModel_ModeChanged;
			this.Scanner.IsScanning = true;
			this.Scanner.IsAnalyzing = true;
		}

		/// <summary>
		/// Asynchronous OnAppearing-method.
		/// </summary>
		protected override async Task OnDisappearingAsync()
		{
			this.Scanner.IsAnalyzing = false;
			this.Scanner.IsScanning = false;
			this.GetViewModel<ScanQrCodeViewModel>().ModeChanged -= this.ViewModel_ModeChanged;

			await base.OnDisappearingAsync();
		}

		private void ViewModel_ModeChanged(object sender, EventArgs e)
		{
			if (this.GetViewModel<ScanQrCodeViewModel>().ScanIsManual)
				this.LinkEntry.Focus();
		}

		private void Scanner_OnScanResult(Result result)
		{
			if (!string.IsNullOrWhiteSpace(result.Text))
			{
				this.Scanner.IsAnalyzing = false; // Stop analysis until we navigate away so we don't keep reading qr codes
				string Url = result.Text?.Trim();

				this.GetViewModel<ScanQrCodeViewModel>().Url = Url;
				QrCode.TrySetResultAndClosePage(this.ViewModel.NavigationService, this.ViewModel.UiSerializer, Url);
			}
		}

		private async void OpenButton_Click(object sender, EventArgs e)
		{
			string Url = this.GetViewModel<ScanQrCodeViewModel>().LinkText?.Trim();
			try
			{
				string Scheme = Constants.UriSchemes.GetScheme(Url);

				if (string.IsNullOrWhiteSpace(Scheme))
				{
					await this.ViewModel.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnsupportedUriScheme, AppResources.Ok);
					return;
				}
			}
			catch (Exception ex)
			{
				await this.ViewModel.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
				return;
			}

			QrCode.TrySetResultAndClosePage(this.ViewModel.NavigationService, this.ViewModel.UiSerializer, Url);
		}

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled</returns>
		protected override bool OnBackButtonPressed()
		{
			QrCode.TrySetResultAndClosePage(this.ViewModel.NavigationService, this.ViewModel.UiSerializer, string.Empty);
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

			double DisplayOrientationHeight = this.Scanner.Width;
			double DisplayOrientationWidth = this.Scanner.Height;

			double TargetRatio = DisplayOrientationHeight / DisplayOrientationWidth;
			double TargetHeight = DisplayOrientationHeight;
			double MinDiff = double.MaxValue;

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
