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
        private readonly INavigationService navigationService;
        public event EventHandler CodeScanned;
        public event EventHandler ModeChanged;

        public AddPartViewModel()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
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
            ModeText = ScanIsAutomatic ? AppResources.QrScanCode : AppResources.QrEnterManually;
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
                        await this.navigationService.DisplayAlert(AppResources.ErrorTitle, "Not a legal identity.", AppResources.Ok);
                        return;
                    }

                    code = code.Substring(i + 1);
                }

                Code = code;
                this.OnCodeScanned(new CodeScannedEventArgs(code));
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
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