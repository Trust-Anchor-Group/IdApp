using System;
using System.ComponentModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using XamarinApp.Services;
using ZXing;

namespace XamarinApp.Views
{
	[DesignTimeVisible(true)]
	public partial class ScanQrCodePage : ContentPage, IBackButton
    {
        private readonly ITagService tagService;
		private readonly Page owner;

		public ScanQrCodePage(Page Owner)
		{
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.owner = Owner;
			this.BindingContext = this;
		}

		private void BackButton_Clicked(object sender, EventArgs e)
		{
			App.ShowPage(this.owner, true);
		}

		private void ModeButton_Clicked(object sender, EventArgs e)
		{
			bool Manual = !this.ManualGrid.IsVisible;

			this.ScanGrid.IsVisible = !Manual;
			this.ManualGrid.IsVisible = Manual;

			this.ModeButton.Text = Manual ? AppResources.QrScanCodeText : AppResources.QrEnterManuallyText;

			if (Manual)
				this.Link.Focus();
		}

		public void Scanner_OnScanResult(Result result)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				this.Link.Text = result.Text;
				this.ScanGrid.IsVisible = false;
				this.ManualGrid.IsVisible = true;
				this.ModeButton.Text = AppResources.QrScanCodeText;
				this.ManualButton.Focus();
			});
		}

		private async void ManualButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				string Code = this.Link.Text;
				Uri Uri = new Uri(Code);

				switch (Uri.Scheme.ToLower())
				{
					case Constants.Schemes.IotId:
						string LegalId = Code.Substring(6);
						App.ShowPage(this.owner, true);
						await App.OpenLegalIdentity(LegalId, "Scanned QR Code.");
						break;

					case Constants.Schemes.IotSc:
						string ContractId = Code.Substring(6);
						App.ShowPage(this.owner, true);
						await App.OpenContract(ContractId, "Scanned QR Code.");
						break;

					case Constants.Schemes.IotDisco:
						// TODO
						break;

					default:
						if (!await Launcher.TryOpenAsync(Uri))
							await this.DisplayAlert(AppResources.ErrorTitleText,  $"Code not understood:{Environment.NewLine}{Environment.NewLine}{Code}", AppResources.OkButtonText);
						break;
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert(AppResources.ErrorTitleText, ex.Message, AppResources.OkButtonText);
			}
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, EventArgs.Empty);
			return true;
		}

	}
}
