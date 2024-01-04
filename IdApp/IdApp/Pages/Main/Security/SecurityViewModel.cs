using IdApp.Popups.Pin.ChangePin;
using IdApp.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Security
{
	/// <summary>
	/// The view model to bind to for when displaying security options.
	/// </summary>
	public class SecurityViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="SecurityViewModel"/> class.
		/// </summary>
		public SecurityViewModel()
			: base()
		{
			this.ChangePinCommand = new Command(async _ => await this.ExecuteChangePin(), _ => this.IsConnected);
			this.PermitScreenCaptureCommand = new Command(async _ => await this.ExecutePermitScreenCapture());
			this.ProhibitScreenCaptureCommand = new Command(async _ => await this.ExecuteProhibitScreenCapture());
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.CanProhibitScreenCapture = App.CanProhibitScreenCapture;
			this.CanEnableScreenCapture = App.CanProhibitScreenCapture && App.ProhibitScreenCapture;
			this.CanDisableScreenCapture = App.CanProhibitScreenCapture && !App.ProhibitScreenCapture;
		}

		#region Properties

		/// <summary>
		/// See <see cref="CanProhibitScreenCapture"/>
		/// </summary>
		public static readonly BindableProperty CanProhibitScreenCaptureProperty =
			BindableProperty.Create(nameof(CanProhibitScreenCapture), typeof(bool), typeof(SecurityViewModel), default(bool));

		/// <summary>
		/// If screen capture prohibition can be controlled
		/// </summary>
		public bool CanProhibitScreenCapture
		{
			get => (bool)this.GetValue(CanProhibitScreenCaptureProperty);
			set => this.SetValue(CanProhibitScreenCaptureProperty, value);
		}

		/// <summary>
		/// See <see cref="CanEnableScreenCapture"/>
		/// </summary>
		public static readonly BindableProperty CanEnableScreenCaptureProperty =
			BindableProperty.Create(nameof(CanEnableScreenCapture), typeof(bool), typeof(SecurityViewModel), default(bool));

		/// <summary>
		/// If Screen Capture can be enabled.
		/// </summary>
		public bool CanEnableScreenCapture
		{
			get => (bool)this.GetValue(CanEnableScreenCaptureProperty);
			set => this.SetValue(CanEnableScreenCaptureProperty, value);
		}

		/// <summary>
		/// See <see cref="CanDisableScreenCapture"/>
		/// </summary>
		public static readonly BindableProperty CanDisableScreenCaptureProperty =
			BindableProperty.Create(nameof(CanDisableScreenCapture), typeof(bool), typeof(SecurityViewModel), default(bool));

		/// <summary>
		/// If Screen Capture can be disabled.
		/// </summary>
		public bool CanDisableScreenCapture
		{
			get => (bool)this.GetValue(CanDisableScreenCaptureProperty);
			set => this.SetValue(CanDisableScreenCaptureProperty, value);
		}

		#endregion

		#region Commands

		/// <summary>
		/// Command executed when user wants to change PIN.
		/// </summary>
		public ICommand ChangePinCommand { get; }

		/// <summary>
		/// Command executed when user wants to permit screen capture
		/// </summary>
		public ICommand PermitScreenCaptureCommand { get; }

		/// <summary>
		/// Command executed when user wants to prohibit screen capture
		/// </summary>
		public ICommand ProhibitScreenCaptureCommand { get; }

		private async Task ExecuteChangePin()
		{
			await ChangePin(this);
		}

		internal static async Task ChangePin(IServiceReferences References)
		{
			try
			{
				while (true)
				{
					ChangePinPopupPage Page = new();

					await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(Page);
					(string OldPin, string NewPin) = await Page.Result;

					if (OldPin is null || OldPin == NewPin)
						return;

					if (!References.TagProfile.HasPin ||
						References.TagProfile.ComputePinHash(OldPin) == References.TagProfile.PinHash)
					{
						string NewPassword = References.CryptoService.CreateRandomPassword();

						if (!await References.XmppService.ChangePassword(NewPassword))
						{
							await References.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToChangePassword"]);
							return;
						}

						References.TagProfile.Pin = NewPin;
						await References.TagProfile.SetAccount(References.TagProfile.Account, NewPassword, string.Empty);
						await References.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["PinChanged"]);
						return;
					}

					await References.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PinIsInvalid"]);

					// TODO: Limit number of attempts.
				}
			}
			catch (Exception ex)
			{
				References.LogService.LogException(ex);
				await References.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ExecutePermitScreenCapture()
		{
			if (!App.CanProhibitScreenCapture)
				return;

			if (!await App.VerifyPin())
				return;

			App.ProhibitScreenCapture = false;
			this.CanEnableScreenCapture = false;
			this.CanDisableScreenCapture = true;
		}

		private async Task ExecuteProhibitScreenCapture()
		{
			if (!App.CanProhibitScreenCapture)
				return;

			if (!await App.VerifyPin())
				return;

			App.ProhibitScreenCapture = true;
			this.CanEnableScreenCapture = true;
			this.CanDisableScreenCapture = false;
		}

		#endregion
	}
}
