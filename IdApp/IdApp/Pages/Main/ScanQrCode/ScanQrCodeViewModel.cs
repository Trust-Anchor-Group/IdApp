using System;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.Navigation;
using Xamarin.Forms;

namespace IdApp.Pages.Main.ScanQrCode
{
    /// <summary>
    /// The view model to bind to when scanning a QR code.
    /// </summary>
    public class ScanQrCodeViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// An event that is fired when the scanning mode changes from automatic scan to manual entry.
        /// </summary>
        public event EventHandler ModeChanged;

        /// <summary>
        /// Creates a new instance of the <see cref="ScanQrCodeViewModel"/> class.
        /// </summary>
        public ScanQrCodeViewModel()
            : this(null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ScanQrCodeViewModel"/> class.
        /// For unit tests.
        /// </summary>
        protected internal ScanQrCodeViewModel(INavigationService navigationService)
        {
            SwitchModeCommand = new Command(SwitchMode);
            OpenCommandText = AppResources.Open;
            SetModeText();
            this.navigationService = navigationService ?? App.Instantiate<INavigationService>();
        }

        /// <inheritdoc />
        protected override async Task DoBind()
        {
            await base.DoBind();
            if (this.navigationService.TryPopArgs(out ScanQrCodeNavigationArgs args) && !string.IsNullOrWhiteSpace(args.CommandName))
            {
                OpenCommandText = args.CommandName;
            }
            else
            {
                OpenCommandText = AppResources.Open;
            }
        }

        #region Properties

        /// <summary>
        /// Action to bind to for switching scan mode from manual to automatic.
        /// </summary>
        public ICommand SwitchModeCommand { get; }

        /// <summary>
        /// See <see cref="LinkText"/>
        /// </summary>
        public static readonly BindableProperty LinkTextProperty =
            BindableProperty.Create("LinkText", typeof(string), typeof(ScanQrCodeViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ScanQrCodeViewModel viewModel = (ScanQrCodeViewModel)b;
                viewModel.OpenIsEnabled = !string.IsNullOrWhiteSpace((string)newValue);
            });

        /// <summary>
        /// The link text, i.e. the full qr code including scheme.
        /// </summary>
        public string LinkText
        {
            get { return (string)GetValue(LinkTextProperty); }
            set { SetValue(LinkTextProperty, value); }
        }

        /// <summary>
        /// See <see cref="Url"/>
        /// </summary>
        public static readonly BindableProperty UrlProperty =
            BindableProperty.Create("Url", typeof(string), typeof(ScanQrCodeViewModel), default(string));

        /// <summary>
        /// The raw QR code URL.
        /// </summary>
        public string Url
        {
            get { return (string)GetValue(UrlProperty); }
            set { SetValue(UrlProperty, value); }
        }

        /// <summary>
        /// See <see cref="OpenCommandText"/>
        /// </summary>
        public static readonly BindableProperty OpenCommandTextProperty =
            BindableProperty.Create("OpenCommandText", typeof(string), typeof(ScanQrCodeViewModel), default(string));

        /// <summary>
        /// The localized, user friendly command name to display in the UI for scanning a QR Code. Typically "Add" or "Open".
        /// </summary>
        public string OpenCommandText
        {
            get { return (string) GetValue(OpenCommandTextProperty); }
            set { SetValue(OpenCommandTextProperty, value); }
        }

        /// <summary>
        /// See <see cref="OpenIsEnabled"/>
        /// </summary>
        public static readonly BindableProperty OpenIsEnabledProperty =
            BindableProperty.Create("OpenIsEnabled", typeof(bool), typeof(ScanQrCodeViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the open command is enabled or not.
        /// </summary>
        public bool OpenIsEnabled
        {
            get { return (bool) GetValue(OpenIsEnabledProperty); }
            set { SetValue(OpenIsEnabledProperty, value); }
        }

        /// <summary>
        /// See <see cref="ScanIsAutomatic"/>
        /// </summary>
        public static readonly BindableProperty ScanIsAutomaticProperty =
            BindableProperty.Create("ScanIsAutomatic", typeof(bool), typeof(ScanQrCodeViewModel), true, propertyChanged: (b, oldValue, newValue) =>
            {
                ScanQrCodeViewModel viewModel = (ScanQrCodeViewModel)b;
                viewModel.ScanIsManual = !(bool)newValue;
                viewModel.SetModeText();
            });

        /// <summary>
        /// Gets or sets whether the QR scanning is automatic or manual. <seealso cref="ScanIsManual"/>.
        /// </summary>
        public bool ScanIsAutomatic
        {
            get { return (bool)GetValue(ScanIsAutomaticProperty); }
            set { SetValue(ScanIsAutomaticProperty, value); }
        }

        /// <summary>
        /// <see cref="ScanIsManual"/>
        /// </summary>
        public static readonly BindableProperty ScanIsManualProperty =
            BindableProperty.Create("ScanIsManual", typeof(bool), typeof(ScanQrCodeViewModel), false);

        /// <summary>
        /// Gets or sets whether the QR scanning is automatic or manual. <seealso cref="ScanIsAutomatic"/>.
        /// </summary>
        public bool ScanIsManual
        {
            get { return (bool)GetValue(ScanIsManualProperty); }
            set { SetValue(ScanIsManualProperty, value); }
        }

        /// <summary>
        /// See <see cref="ModeText"/>
        /// </summary>
        public static readonly BindableProperty ModeTextProperty =
            BindableProperty.Create("ModeText", typeof(string), typeof(ScanQrCodeViewModel), default(string));

        /// <summary>
        /// The localized mode text to display to the user.
        /// </summary>
        public string ModeText
        {
            get { return (string)GetValue(ModeTextProperty); }
            set { SetValue(ModeTextProperty, value); }
        }

        #endregion

        private void SetModeText()
        {
            ModeText = ScanIsAutomatic ? AppResources.QrEnterManually : AppResources.QrScanCode;
        }

        private void SwitchMode()
        {
            ScanIsAutomatic = !ScanIsAutomatic;
            OnModeChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Invoke this method to fire the <see cref="ModeChanged"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnModeChanged(EventArgs e)
        {
            ModeChanged?.Invoke(this, e);
        }
    }
}