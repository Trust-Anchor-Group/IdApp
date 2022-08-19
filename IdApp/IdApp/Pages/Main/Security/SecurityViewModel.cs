using IdApp.Popups.Pin.ChangePin;
using IdApp.Resx;
using IdApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Operators.Vectors;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Security
{
	/// <summary>
	/// The view model to bind to for when displaying the calculator.
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
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			await base.OnDispose();
		}

		#region Properties

		#endregion

		#region Commands

		/// <summary>
		/// Command executed when user wants to change PIN.
		/// </summary>
		public ICommand ChangePinCommand { get; }

		private async Task ExecuteChangePin()
		{
			await ChangePin(this);
		}

		internal static async Task ChangePin(ServiceReferences References)
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

					if (References.TagProfile.ComputePinHash(OldPin) == References.TagProfile.PinHash)
					{
						TaskCompletionSource<bool> PasswordChanged = new();
						string NewPassword = References.CryptoService.CreateRandomPassword();

						References.XmppService.Xmpp.ChangePassword(NewPassword, (sender, e) =>
						{
							PasswordChanged.TrySetResult(e.Ok);
							return Task.CompletedTask;
						}, null);

						if (!await PasswordChanged.Task)
						{
							await References.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToChangePassword);
							return;
						}

						References.TagProfile.Pin = NewPin;
						References.TagProfile.SetAccount(References.TagProfile.Account, NewPassword, string.Empty);

						await References.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.PinChanged);
						return;
					}

					await References.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);

					// TODO: Limit number of attempts.
				}
			}
			catch (Exception ex)
			{
				References.LogService.LogException(ex);
				await References.UiSerializer.DisplayAlert(ex);
			}
		}

		#endregion
	}
}
