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
        private readonly bool isModal;
        private readonly INavigationService navigationService;

		public AddPartPage(Page Owner, StringEventHandler Callback, bool isModal)
		{
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.callback = Callback;
            this.isModal = isModal;
			this.ViewModel = new AddPartPageViewModel(Callback, isModal);
            this.navigationService = DependencyService.Resolve<INavigationService>();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            GetViewModel<AddPartPageViewModel>().ModeChanged += ViewModel_ModeChanged;
        }

        protected override void OnDisappearing()
        {
            GetViewModel<AddPartPageViewModel>().ModeChanged -= ViewModel_ModeChanged;
            base.OnDisappearing();
        }

        private void ViewModel_ModeChanged(object sender, EventArgs e)
        {
            if (GetViewModel<AddPartPageViewModel>().ScanIsManual)
            {
                this.LinkEntry.Focus();
            }
        }

        private void BackButton_Clicked(object sender, EventArgs e)
		{
            if (this.isModal)
                this.navigationService.GoBackFromModal();
            else
                this.navigationService.GoBack();
		}

		public async void Scanner_OnScanResult(Result result)
		{
			try
			{
				string s = result.Text;

				if (s.StartsWith(Constants.Schemes.IotId + ":"))
				{
					this.callback?.Invoke(s.Substring(6));
                    if (this.isModal)
                        await this.navigationService.GoBackFromModal();
                    else
                        await this.navigationService.GoBack();
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
