using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Services;
using XamarinApp.Views;

namespace XamarinApp.ViewModels
{
    public class AddPartViewModel : BaseViewModel
    {
        private readonly IMessageService messageService;
        public event EventHandler CodeScanned;
        public event EventHandler ModeChanged;

        public AddPartViewModel()
        {
            this.messageService = DependencyService.Resolve<IMessageService>();
            SwitchModeCommand = new Command(SwitchMode);
            ManualAddCommand = new Command(async () => await PerformManualAdd());
            AutomaticAddCommand = new Command<string>(PerformAutomaticAdd);
            SetModeText();
        }

        public ICommand SwitchModeCommand { get; }

        public ICommand ManualAddCommand { get; }

        public ICommand AutomaticAddCommand { get; }

        public static readonly BindableProperty LinkTextProperty =
            BindableProperty.Create("LinkText", typeof(string), typeof(AddPartViewModel), default(string));

        public string LinkText
        {
            get { return (string)GetValue(LinkTextProperty); }
            set { SetValue(LinkTextProperty, value); }
        }

        public static readonly BindableProperty CodeProperty =
            BindableProperty.Create("Code", typeof(string), typeof(AddPartViewModel), default(string));

        public string Code
        {
            get { return (string)GetValue(CodeProperty); }
            set { SetValue(CodeProperty, value); }
        }

        public static readonly BindableProperty ScanIsAutomaticProperty =
            BindableProperty.Create("ScanIsAutomatic", typeof(bool), typeof(AddPartViewModel), true, propertyChanged: (b, oldValue, newValue) =>
            {
                AddPartViewModel viewModel = (AddPartViewModel)b;
                viewModel.ScanIsManual = !(bool)newValue;
                viewModel.SetModeText();
            });

        private void SetModeText()
        {
            ModeText = ScanIsAutomatic ? AppResources.QrScanCodeText : AppResources.QrEnterManuallyText;
        }

        public bool ScanIsAutomatic
        {
            get { return (bool)GetValue(ScanIsAutomaticProperty); }
            set { SetValue(ScanIsAutomaticProperty, value); }
        }

        public static readonly BindableProperty ScanIsManualProperty =
            BindableProperty.Create("ScanIsManual", typeof(bool), typeof(AddPartViewModel), false);

        public bool ScanIsManual
        {
            get { return (bool)GetValue(ScanIsManualProperty); }
            set { SetValue(ScanIsManualProperty, value); }
        }

        public static readonly BindableProperty ModeTextProperty =
            BindableProperty.Create("ModeText", typeof(string), typeof(AddPartViewModel), default(string));

        public string ModeText
        {
            get { return (string)GetValue(ModeTextProperty); }
            set { SetValue(ModeTextProperty, value); }
        }

        private void SwitchMode()
        {
            ScanIsAutomatic = !ScanIsAutomatic;
            OnModeChanged(EventArgs.Empty);
        }

        private void PerformAutomaticAdd(string code)
        {
            Code = code;
            OnCodeScanned(new CodeScannedEventArgs(code));
        }

        private async Task PerformManualAdd()
        {
            try
            {
                string code = LinkText;
                int i = code.IndexOf(':');

                if (i > 0)
                {
                    if (code.Substring(0, i).ToLower() != Constants.Schemes.IotId)
                    {
                        await this.messageService.DisplayAlert(AppResources.ErrorTitleText, "Not a legal identity.", AppResources.OkButtonText);
                        return;
                    }

                    code = code.Substring(i + 1);
                }

                Code = code;
                this.OnCodeScanned(new CodeScannedEventArgs(code));
            }
            catch (Exception ex)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitleText, ex.Message, AppResources.OkButtonText);
            }
        }

        protected virtual void OnModeChanged(EventArgs e)
        {
            ModeChanged?.Invoke(this, e);
        }

        protected virtual void OnCodeScanned(CodeScannedEventArgs e)
        {
            CodeScanned?.Invoke(this, e);
        }
    }
}