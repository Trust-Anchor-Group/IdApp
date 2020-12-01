using System;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels
{
    public class ScanQrCodeViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;
        public event EventHandler ModeChanged;

        public ScanQrCodeViewModel()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            SwitchModeCommand = new Command(SwitchMode);
            OpenCommandText = AppResources.Open;
            SetModeText();
        }

        public ICommand SwitchModeCommand { get; }

        public static readonly BindableProperty LinkTextProperty =
            BindableProperty.Create("LinkText", typeof(string), typeof(ScanQrCodeViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ScanQrCodeViewModel viewModel = (ScanQrCodeViewModel)b;
                viewModel.OpenIsEnabled = !string.IsNullOrWhiteSpace((string)newValue);
            });

        public string LinkText
        {
            get { return (string)GetValue(LinkTextProperty); }
            set { SetValue(LinkTextProperty, value); }
        }

        public static readonly BindableProperty CodeProperty =
            BindableProperty.Create("Code", typeof(string), typeof(ScanQrCodeViewModel), default(string));

        public string Code
        {
            get { return (string)GetValue(CodeProperty); }
            set { SetValue(CodeProperty, value); }
        }

        public static readonly BindableProperty OpenCommandTextProperty =
            BindableProperty.Create("OpenCommandText", typeof(string), typeof(ScanQrCodeViewModel), default(string));

        public string OpenCommandText
        {
            get { return (string) GetValue(OpenCommandTextProperty); }
            set { SetValue(OpenCommandTextProperty, value); }
        }

        public static readonly BindableProperty OpenIsEnabledProperty =
            BindableProperty.Create("OpenIsEnabled", typeof(bool), typeof(ScanQrCodeViewModel), default(bool));

        public bool OpenIsEnabled
        {
            get { return (bool) GetValue(OpenIsEnabledProperty); }
            set { SetValue(OpenIsEnabledProperty, value); }
        }

        public static readonly BindableProperty ScanIsAutomaticProperty =
            BindableProperty.Create("ScanIsAutomatic", typeof(bool), typeof(ScanQrCodeViewModel), true, propertyChanged: (b, oldValue, newValue) =>
            {
                ScanQrCodeViewModel viewModel = (ScanQrCodeViewModel)b;
                viewModel.ScanIsManual = !(bool)newValue;
                viewModel.SetModeText();
            });

        private void SetModeText()
        {
            ModeText = ScanIsAutomatic ? AppResources.QrEnterManually : AppResources.QrScanCode;
        }

        public bool ScanIsAutomatic
        {
            get { return (bool)GetValue(ScanIsAutomaticProperty); }
            set { SetValue(ScanIsAutomaticProperty, value); }
        }

        public static readonly BindableProperty ScanIsManualProperty =
            BindableProperty.Create("ScanIsManual", typeof(bool), typeof(ScanQrCodeViewModel), false);

        public bool ScanIsManual
        {
            get { return (bool)GetValue(ScanIsManualProperty); }
            set { SetValue(ScanIsManualProperty, value); }
        }

        public static readonly BindableProperty ModeTextProperty =
            BindableProperty.Create("ModeText", typeof(string), typeof(ScanQrCodeViewModel), default(string));

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

        protected virtual void OnModeChanged(EventArgs e)
        {
            ModeChanged?.Invoke(this, e);
        }
    }
}