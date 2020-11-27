using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;

namespace XamarinApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanQrCodePage
    {
        public event EventHandler<OpenEventArgs> Open;
        private readonly bool isModal;
        private readonly INavigationService navigationService;

        public ScanQrCodePage(bool isModal)
        {
            InitializeComponent();
            this.isModal = isModal;
            this.navigationService = DependencyService.Resolve<INavigationService>();
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
#pragma warning disable 4014
            ClosePage();
#pragma warning restore 4014
            Open?.Invoke(this, new OpenEventArgs(ScanView.ScannedCode));
        }

        private async void BackButton_Click(object sender, EventArgs e)
        {
            await ClosePage();
        }


        private async Task ClosePage()
        {
            if (isModal)
            {
                await this.navigationService.PopModalAsync();
            }
            else
            {
                await this.navigationService.PopAsync();
            }
        }

        protected override bool OnBackButtonPressed()
        {
#pragma warning disable 4014
            ClosePage();
#pragma warning restore 4014
            return true;
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