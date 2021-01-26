using System;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels;
using ZXing;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanQrCodePage
    {
        private readonly IUiDispatcher uiDispatcher;
        private readonly INavigationService navigationService;

        public ScanQrCodePage()
            : this(null)
        {
        }

        protected internal ScanQrCodePage(ScanQrCodeViewModel viewModel)
        {
            this.ViewModel = viewModel ?? new ScanQrCodeViewModel();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            InitializeComponent();
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
                string code = result.Text;
                GetViewModel<ScanQrCodeViewModel>().Code = code;
                QrCode.TrySetResultAndClosePage(this.navigationService, this.uiDispatcher, code);
            }
        }

        private async void OpenButton_Click(object sender, EventArgs e)
        {
            string code = GetViewModel<ScanQrCodeViewModel>().LinkText;
            try
            {
                string scheme = Constants.IoTSchemes.GetScheme(code);

                if (!string.IsNullOrWhiteSpace(scheme))
                {
                    if (scheme != Constants.IoTSchemes.IotId)
                    {
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity, AppResources.Ok);
                        return;
                    }
                }
                else
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.TheSpecifiedCodeIsNotALegalIdentity, AppResources.Ok);
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

        protected override bool OnBackButtonPressed()
        {
            QrCode.TrySetResultAndClosePage(this.navigationService, this.uiDispatcher, string.Empty);
            return true;
        }
    }
}