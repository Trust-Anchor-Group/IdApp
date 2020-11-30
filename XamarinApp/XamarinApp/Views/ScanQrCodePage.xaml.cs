using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanQrCodePage
    {
        public event EventHandler<OpenEventArgs> Open;
        private readonly INavigationService navigationService;

        public ScanQrCodePage()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.BindingContext = this;
            InitializeComponent();
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            Open?.Invoke(this, new OpenEventArgs(ScanView.ScannedCode));
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }

        private void ScanView_CodeScanned(object sender, CodeScannedEventArgs e)
        {
            Open?.Invoke(this, new OpenEventArgs(ScanView.ScannedCode));
        }
    }

    public class OpenEventArgs : EventArgs
    {
        public OpenEventArgs(string code)
        {
            this.Code = code;
        }

        public string Code { get; }
    }
}