using System;
using System.ComponentModel;
using Xamarin.Forms;
using ZXing;

namespace XamarinApp.MainMenu.Contracts
{
	/// <summary>
	/// Delegate for string-valued callback methods.
	/// </summary>
	/// <param name="Value">Value</param>
	public delegate void StringEventHandler(string Value);

	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(true)]
	public partial class AddPartPage : ContentPage, IBackButton
	{
		private readonly StringEventHandler callback;
		private readonly Page owner;

		public AddPartPage(Page Owner, StringEventHandler Callback)
		{
			this.owner = Owner;
			this.callback = Callback;
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

		public async void Scanner_OnScanResult(Result result)
		{
			try
			{
				string s = result.Text;

				if (s.StartsWith("iotid:"))
				{
					this.callback?.Invoke(s.Substring(6));
					App.ShowPage(this.owner, true);
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		private async void ManualButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				string Code = this.Link.Text;
				int i = Code.IndexOf(':');

				if (i > 0)
				{
					if (Code.Substring(0, i).ToLower() != "iotid")
					{
						await this.DisplayAlert("Error", "Not a legal identity.", "OK");
						return;
					}

					Code = Code.Substring(i + 1);
				}

				this.callback?.Invoke(Code);
				App.ShowPage(this.owner, true);
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
