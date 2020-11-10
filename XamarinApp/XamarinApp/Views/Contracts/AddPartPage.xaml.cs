using System;
using System.ComponentModel;
using Xamarin.Forms;
using XamarinApp.Services;
using XamarinApp.ViewModels.Contracts;
using ZXing;

namespace XamarinApp.Views.Contracts
{
	/// <summary>
	/// Delegate for string-valued callback methods.
	/// </summary>
	/// <param name="Value">Value</param>
	public delegate void StringEventHandler(string Value);

	[DesignTimeVisible(true)]
	public partial class AddPartPage : IBackButton
    {
        private readonly ITagService tagService;
		private readonly StringEventHandler callback;
		private readonly Page owner;

		public AddPartPage(Page Owner, StringEventHandler Callback)
		{
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.owner = Owner;
			this.callback = Callback;
			this.ViewModel = new AddPartPageViewModel(Callback);
		}

		private void BackButton_Clicked(object sender, EventArgs e)
		{
			App.ShowPage(this.owner, true);
		}

		public async void Scanner_OnScanResult(Result result)
		{
			try
			{
				string s = result.Text;

				if (s.StartsWith(Constants.Schemes.IotId + ":"))
				{
					this.callback?.Invoke(s.Substring(6));
					App.ShowPage(this.owner, true);
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
