using System;
using System.ComponentModel;
using Waher.Events;
using Xamarin.Essentials;
using Xamarin.Forms;
using XamarinApp.Services;
using ZXing;

namespace XamarinApp.Views
{
	[DesignTimeVisible(true)]
	public partial class ScanQrCodePage : IBackButton
    {
        private readonly ITagService tagService;
		private readonly Page owner;
        private readonly bool modal;
        private string result = string.Empty;

		public ScanQrCodePage(Page Owner, bool modal)
		{
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.owner = Owner;
			this.BindingContext = this;
            this.modal = modal;
        }

		private void BackButton_Clicked(object sender, EventArgs e)
		{
            if (this.modal)
                this.Navigation.PopModalAsync();
            else
                App.ShowPage(this.owner, true);
        }

		private void ModeButton_Clicked(object sender, EventArgs e)
		{
			bool Manual = !this.ManualGrid.IsVisible;

			this.ScanGrid.IsVisible = !Manual;
			this.ManualGrid.IsVisible = Manual;

			this.ModeButton.Text = Manual ? AppResources.QrScanCode : AppResources.QrEnterManually;

			if (Manual)
				this.Link.Focus();
		}

        public string Result => this.Link.Text;
        public event EventHandler CodeScanned = null;

		public void Scanner_OnScanResult(Result result)
		{
            this.result = result.Text;

            if (!(string.IsNullOrEmpty(result?.Text)))
            {
                if (this.modal)
                {
                    try
                    {
                        CodeScanned?.Invoke(this, new EventArgs());
                    }
                    catch (Exception ex)
                    {
                        Log.Critical(ex);
                    }

                    this.BackClicked();
                }
                else
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        this.Link.Text = result.Text;
                        this.ScanGrid.IsVisible = false;
                        this.ManualGrid.IsVisible = true;
                        this.ModeButton.Text = "Scan Code";
                        this.OpenButton.Focus();
                    });
                }
            }
        }

		private async void OpenButton_Clicked(object sender, EventArgs e)
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
							await this.DisplayAlert(AppResources.ErrorTitle,  $"Code not understood:{Environment.NewLine}{Environment.NewLine}{Code}", AppResources.Ok);
						break;
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, EventArgs.Empty);
			return true;
		}

	}
}
