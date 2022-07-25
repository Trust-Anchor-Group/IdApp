using IdApp.Resx;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace IdApp.Pages.Main.ScanQrCode
{
    /// <summary>
    /// The view model to bind to when scanning a QR code.
    /// </summary>
    public class ScanQrCodeViewModel : BaseViewModel
    {
		private readonly ScanQrCodeNavigationArgs navigationArgs;

		/// <summary>
		/// An event that is fired when the scanning mode changes from automatic scan to manual entry.
		/// </summary>
		public event EventHandler ModeChanged;

        /// <summary>
        /// Creates a new instance of the <see cref="ScanQrCodeViewModel"/> class.
        /// </summary>
        public ScanQrCodeViewModel(ScanQrCodeNavigationArgs NavigationArgs)
        {
			this.navigationArgs = NavigationArgs;
			this.SwitchModeCommand = new Command(this.SwitchMode);
			this.OpenCommandText = AppResources.Open;
			this.SetModeText();
        }

        /// <inheritdoc />
        protected override async Task DoBind()
        {
            await base.DoBind();

			ScanQrCodeNavigationArgs NavigationArgs = this.navigationArgs
				?? (this.NavigationService.TryPopArgs(out ScanQrCodeNavigationArgs Args) ? Args : null);

			this.OpenCommandText = !string.IsNullOrWhiteSpace(NavigationArgs?.CommandName)
				? NavigationArgs.CommandName
				: AppResources.Open;
        }

        /// <inheritdoc />
		protected override Task DoUnbind()
		{
            return base.DoUnbind();
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
            BindableProperty.Create(nameof(LinkText), typeof(string), typeof(ScanQrCodeViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                ScanQrCodeViewModel viewModel = (ScanQrCodeViewModel)b;
                viewModel.OpenIsEnabled = !string.IsNullOrWhiteSpace((string)newValue);
            });

        /// <summary>
        /// The link text, i.e. the full qr code including scheme.
        /// </summary>
        public string LinkText
        {
            get => (string)this.GetValue(LinkTextProperty);
            set => this.SetValue(LinkTextProperty, value);
        }

        /// <summary>
        /// See <see cref="Url"/>
        /// </summary>
        public static readonly BindableProperty UrlProperty =
            BindableProperty.Create(nameof(Url), typeof(string), typeof(ScanQrCodeViewModel), default(string));

        /// <summary>
        /// The raw QR code URL.
        /// </summary>
        public string Url
        {
            get => (string)this.GetValue(UrlProperty);
            set => this.SetValue(UrlProperty, value);
        }

        /// <summary>
        /// See <see cref="OpenCommandText"/>
        /// </summary>
        public static readonly BindableProperty OpenCommandTextProperty =
            BindableProperty.Create(nameof(OpenCommandText), typeof(string), typeof(ScanQrCodeViewModel), default(string));

        /// <summary>
        /// The localized, user friendly command name to display in the UI for scanning a QR Code. Typically "Add" or "Open".
        /// </summary>
        public string OpenCommandText
        {
            get => (string)this.GetValue(OpenCommandTextProperty);
            set => this.SetValue(OpenCommandTextProperty, value);
        }

        /// <summary>
        /// See <see cref="OpenIsEnabled"/>
        /// </summary>
        public static readonly BindableProperty OpenIsEnabledProperty =
            BindableProperty.Create(nameof(OpenIsEnabled), typeof(bool), typeof(ScanQrCodeViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the open command is enabled or not.
        /// </summary>
        public bool OpenIsEnabled
        {
            get => (bool)this.GetValue(OpenIsEnabledProperty);
            set => this.SetValue(OpenIsEnabledProperty, value);
        }

        /// <summary>
        /// See <see cref="ScanIsAutomatic"/>
        /// </summary>
        public static readonly BindableProperty ScanIsAutomaticProperty =
            BindableProperty.Create(nameof(ScanIsAutomatic), typeof(bool), typeof(ScanQrCodeViewModel), true, propertyChanged: (b, oldValue, newValue) =>
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
            get => (bool)this.GetValue(ScanIsAutomaticProperty);
            set => this.SetValue(ScanIsAutomaticProperty, value);
        }

        /// <summary>
        /// <see cref="ScanIsManual"/>
        /// </summary>
        public static readonly BindableProperty ScanIsManualProperty =
            BindableProperty.Create(nameof(ScanIsManual), typeof(bool), typeof(ScanQrCodeViewModel), false);

        /// <summary>
        /// Gets or sets whether the QR scanning is automatic or manual. <seealso cref="ScanIsAutomatic"/>.
        /// </summary>
        public bool ScanIsManual
        {
            get => (bool)this.GetValue(ScanIsManualProperty);
            set => this.SetValue(ScanIsManualProperty, value);
        }

        /// <summary>
        /// See <see cref="ModeText"/>
        /// </summary>
        public static readonly BindableProperty ModeTextProperty =
            BindableProperty.Create(nameof(ModeText), typeof(string), typeof(ScanQrCodeViewModel), default(string));

        /// <summary>
        /// The localized mode text to display to the user.
        /// </summary>
        public string ModeText
        {
            get => (string)this.GetValue(ModeTextProperty);
            set => this.SetValue(ModeTextProperty, value);
        }

        #endregion

        private void SetModeText()
        {
			this.ModeText = this.ScanIsAutomatic ? AppResources.QrEnterManually : AppResources.QrScanCode;
        }

        private void SwitchMode()
        {
			this.ScanIsAutomatic = !this.ScanIsAutomatic;
			this.OnModeChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Invoke this method to fire the <see cref="ModeChanged"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnModeChanged(EventArgs e)
        {
            this.ModeChanged?.Invoke(this, e);
        }
    }
}
