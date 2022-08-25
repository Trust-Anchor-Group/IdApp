using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Main.ScanQrCode
{
    /// <summary>
    /// The view model to bind to when scanning a QR code.
    /// </summary>
    public class ScanQrCodeViewModel : BaseViewModel
    {
		private ScanQrCodeNavigationArgs navigationArgs;
		private bool useShellNavigationService;

		/// <summary>
		/// An event that is fired when the scanning mode changes from automatic scan to manual entry.
		/// </summary>
		public event EventHandler ModeChanged;

        /// <summary>
        /// Creates a new instance of the <see cref="ScanQrCodeViewModel"/> class.
        /// </summary>
        public ScanQrCodeViewModel(ScanQrCodeNavigationArgs NavigationArgs)
        {
			this.useShellNavigationService = NavigationArgs is null;
			this.navigationArgs = NavigationArgs;
			this.SwitchModeCommand = new Command(this.SwitchMode);
			this.OpenCommandText = LocalizationResourceManager.Current["Open"];
			this.SetModeText();
        }

		/// <inheritdoc />
		protected override async Task OnInitialize()
        {
            await base.OnInitialize();

			if (this.navigationArgs is null && this.NavigationService.TryPopArgs(out ScanQrCodeNavigationArgs Args))
			{
				this.navigationArgs = Args;
				this.useShellNavigationService = Args is not null;
			}

			this.OpenCommandText = !string.IsNullOrWhiteSpace(this.navigationArgs?.CommandName)
				? this.navigationArgs.CommandName
				: LocalizationResourceManager.Current["Open"];
        }

		/// <summary>
		/// Tries to set the Scan QR Code result and close the scan page.
		/// </summary>
		/// <param name="Url">The URL to set.</param>
		internal async void TrySetResultAndClosePage(string Url)
		{
			if (this.navigationArgs is null)
			{
				try
				{
					if (this.useShellNavigationService)
						await this.NavigationService.GoBackAsync();
					else
						await App.Current.MainPage.Navigation.PopAsync();
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
			else
			{
				Func<string, Task> Action = this.navigationArgs.Action;
				TaskCompletionSource<string> QrCodeScanned = this.navigationArgs.QrCodeScanned;

				this.navigationArgs.Action = null;
				this.navigationArgs.QrCodeScanned = null;

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						Url = Url?.Trim();

						if (Action is not null)
						{
							try
							{
								await Action(Url);
							}
							catch (Exception ex)
							{
								this.LogService.LogException(ex);
							}
						}

						if (this.useShellNavigationService)
							await this.NavigationService.GoBackAsync();
						else
							await App.Current.MainPage.Navigation.PopAsync();

						if (QrCodeScanned is not null)
							QrCodeScanned?.TrySetResult(Url);
					}
					catch (Exception ex)
					{
						this.LogService.LogException(ex);
					}
				});
			}
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			this.TrySetResultAndClosePage(string.Empty);

			await base.OnDispose();
		}

		/// <summary>
		/// A Boolean flag indicating if Shell navigation should be used or a simple <c>PopAsync</c>.
		/// </summary>
		public bool UseShellNavigationService => this.useShellNavigationService;

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
			this.ModeText = this.ScanIsAutomatic ? LocalizationResourceManager.Current["QrEnterManually"] : LocalizationResourceManager.Current["QrScanCode"];
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
