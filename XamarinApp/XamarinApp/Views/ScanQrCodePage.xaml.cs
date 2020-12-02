using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using ZXing;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanQrCodePage
    {
        private readonly INavigationService navigationService;

        public ScanQrCodePage()
            : this(null)
        {
        }

        protected internal ScanQrCodePage(ScanQrCodeViewModel viewModel)
        {
            this.ViewModel = viewModel ?? new ScanQrCodeViewModel();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            InitializeComponent();
        }

        public string OpenCommandText
        {
            get => GetViewModel<ScanQrCodeViewModel>().OpenCommandText;
            set => GetViewModel<ScanQrCodeViewModel>().OpenCommandText = value;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            GetViewModel<ScanQrCodeViewModel>().ModeChanged += ViewModel_ModeChanged;
        }

        protected override void OnDisappearing()
        {
            GetViewModel<ScanQrCodeViewModel>().ModeChanged -= ViewModel_ModeChanged;
            base.OnDisappearing();
        }

        #region Scanning

        TaskCompletionSource<string> qrCodeScanned;

        public Task<string> ScanQrCode()
        {
            _ = this.navigationService.PushAsync(this);
            qrCodeScanned = new TaskCompletionSource<string>();
            return qrCodeScanned.Task;
        }

        #endregion

        private void ViewModel_ModeChanged(object sender, EventArgs e)
        {
            if (GetViewModel<ScanQrCodeViewModel>().ScanIsManual)
            {
                this.LinkEntry.Focus();
            }
        }

        private async void Scanner_OnScanResult(Result result)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                string code = result.Text;
                GetViewModel<ScanQrCodeViewModel>().Code = code;
                await TrySetResultAndClosePage(code);
            }
        }

        private async void OpenButton_Click(object sender, EventArgs e)
        {
            string code = GetViewModel<ScanQrCodeViewModel>().LinkText;
            try
            {
                int i = code.IndexOf(':');

                if (i > 0)
                {
                    if (code.Substring(0, i).ToLower() != Constants.IoTSchemes.IotId)
                    {
                        await this.navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity, AppResources.Ok);
                        return;
                    }
                }
                else
                {
                    await this.navigationService.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity, AppResources.Ok);
                    return;
                }
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
                return;
            }

            await TrySetResultAndClosePage(code);
        }

        private async Task TrySetResultAndClosePage(string code)
        {
            if (!string.IsNullOrWhiteSpace(code) && qrCodeScanned != null)
            {
                qrCodeScanned.TrySetResult(code);
                qrCodeScanned = null;
            }
            await this.navigationService.PopAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            _ = TrySetResultAndClosePage(string.Empty);
            return true;
        }
    }
}