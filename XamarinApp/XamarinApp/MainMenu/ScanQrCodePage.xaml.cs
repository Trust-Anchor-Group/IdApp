using System;
using System.ComponentModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace XamarinApp.MainMenu
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(true)]
	public partial class ScanQrCodePage : ContentPage, IBackButton
	{
		private readonly Page owner;

		public ScanQrCodePage(Page Owner)
		{
			this.owner = Owner;
			this.BindingContext = this;
			InitializeComponent();
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

			this.ModeButton.Text = Manual ? "Scan Code" : "Enter Manually";

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
				this.ModeButton.Text = "Scan Code";
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
					case "iotid":
						string LegalId = Code.Substring(6);
						App.ShowPage(this.owner, true);
						await App.OpenLegalIdentity(LegalId, "Scanned QR Code.");
						break;

					case "iotsc":
						string ContractId = Code.Substring(6);
						App.ShowPage(this.owner, true);
						await App.OpenContract(ContractId, "Scanned QR Code.");
						break;

					case "iotdisco":
						// TODO
						break;

					default:
						if (!await Launcher.TryOpenAsync(Uri))
							await this.DisplayAlert("Error", "Code not understood:\r\n\r\n" + Code, "OK");
						break;
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}

	}
}
