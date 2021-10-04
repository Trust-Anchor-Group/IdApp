using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Xamarin.Forms;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Settings;
using IdApp.Services.Tag;
using IdApp.Services.UI;

namespace IdApp.Pages.Registration.ValidatePhoneNr
{
	/// <summary>
	/// For what purpose the app will be used
	/// </summary>
	public enum PurposeUse
	{
		/// <summary>
		/// For work or personal use
		/// </summary>
		WorkOrPersonal = 0,

		/// <summary>
		/// For educational or experimental use
		/// </summary>
		EducationalOrExperimental = 1
	}

	/// <summary>
	/// The view model to bind to when showing Step 1 of the registration flow: choosing an operator.
	/// </summary>
	public class ValidatePhoneNrViewModel : RegistrationStepViewModel
	{
		private readonly INetworkService networkService;

		/// <summary>
		/// Creates a new instance of the <see cref="ValidatePhoneNrViewModel"/> class.
		/// </summary>
		/// <param name="tagProfile">The tag profile to work with.</param>
		/// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="navigationService">The navigation service to use for app navigation</param>
		/// <param name="settingsService">The settings service for persisting UI state.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="logService">The log service.</param>
		public ValidatePhoneNrViewModel(
			ITagProfile tagProfile,
			IUiSerializer uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			ISettingsService settingsService,
			INetworkService networkService,
			ILogService logService)
			: base(RegistrationStep.ValidatePhoneNr, tagProfile, uiDispatcher, neuronService, navigationService, settingsService, logService)
		{
			this.networkService = networkService;
			this.SendCodeCommand = new Command(async () => await this.SendCode(), this.SendCodeCanExecute);
			this.VerifyCodeCommand = new Command(async () => await this.VerifyCode(), this.VerifyCodeCanExecute);
			this.Title = AppResources.EnterPhoneNumber;
			this.Purposes = new ObservableCollection<string>();
		}

		/// <summary>
		/// Override this method to do view model specific setup when it's parent page/view appears on screen.
		/// </summary>
		protected override async Task DoBind()
		{
			await base.DoBind();

			this.Purposes.Clear();
			this.Purposes.Add(AppResources.PurposeWorkOrPersonal);
			this.Purposes.Add(AppResources.PurposeEducationalOrExperimental);

			if (string.IsNullOrEmpty(this.TagProfile.PhoneNumber))
			{
				try
				{
					object Result = await InternetContent.PostAsync(
						new Uri("https://id.tagroot.io/ID/CountryCode.ws"), string.Empty,
						new KeyValuePair<string, string>("Accept", "application/json"));

					if (Result is Dictionary<string, object> Response &&
						Response.TryGetValue("PhoneCode", out object Obj) && Obj is string PhoneCode)
					{
						this.PhoneNumber = PhoneCode;
					}
					else
						this.PhoneNumber = "+";
				}
				catch (Exception ex)
				{
					this.PhoneNumber = "+";
					this.LogService.LogException(ex);
				}
			}
			else
				this.PhoneNumber = this.TagProfile.PhoneNumber;

			this.EvaluateAllCommands();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.SendCodeCommand, this.VerifyCodeCommand);
		}

		#region Properties

		/// <summary>
		/// Holds the list of purposes to display.
		/// </summary>
		public ObservableCollection<string> Purposes { get; }

		/// <summary>
		/// See <see cref="Purpose"/>
		/// </summary>
		public static readonly BindableProperty PurposeProperty =
			BindableProperty.Create("Purpose", typeof(int), typeof(ValidatePhoneNrViewModel), -1);

		/// <summary>
		/// Phone number
		/// </summary>
		public int Purpose
		{
			get { return (int)GetValue(PurposeProperty); }
			set
			{
				SetValue(PurposeProperty, value);
				this.PurposeSelected = true;
			}
		}

		/// <summary>
		/// See <see cref="PurposeSelected"/>
		/// </summary>
		public static readonly BindableProperty PurposeSelectedProperty =
			BindableProperty.Create("PurposeSelected", typeof(bool), typeof(ValidatePhoneNrViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool PurposeSelected
		{
			get { return (bool)GetValue(PurposeSelectedProperty); }
			set { SetValue(PurposeSelectedProperty, value); }
		}

		/// <summary>
		/// See <see cref="PhoneNumber"/>
		/// </summary>
		public static readonly BindableProperty PhoneNumberProperty =
			BindableProperty.Create("PhoneNumber", typeof(string), typeof(ValidatePhoneNrViewModel), default(string));

		/// <summary>
		/// Phone number
		/// </summary>
		public string PhoneNumber
		{
			get { return (string)GetValue(PhoneNumberProperty); }
			set
			{
				SetValue(PhoneNumberProperty, value);
				SetValue(PhoneNumberValidProperty, this.IsInternationalPhoneNumberFormat(value));
			}
		}

		/// <summary>
		/// See <see cref="PhoneNumberValid"/>
		/// </summary>
		public static readonly BindableProperty PhoneNumberValidProperty =
			BindableProperty.Create("PhoneNumberValid", typeof(bool), typeof(ValidatePhoneNrViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool PhoneNumberValid
		{
			get { return (bool)GetValue(PhoneNumberValidProperty); }
			set { SetValue(PhoneNumberValidProperty, value); }
		}

		/// <summary>
		/// See <see cref="CodeSent"/>
		/// </summary>
		public static readonly BindableProperty CodeSentProperty =
			BindableProperty.Create("CodeSent", typeof(bool), typeof(ValidatePhoneNrViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool CodeSent
		{
			get { return (bool)GetValue(CodeSentProperty); }
			set { SetValue(CodeSentProperty, value); }
		}

		/// <summary>
		/// See <see cref="VerificationCode"/>
		/// </summary>
		public static readonly BindableProperty VerificationCodeProperty =
			BindableProperty.Create("VerificationCode", typeof(string), typeof(ValidatePhoneNrViewModel), default(string));

		/// <summary>
		/// Phone number
		/// </summary>
		public string VerificationCode
		{
			get { return (string)GetValue(VerificationCodeProperty); }
			set
			{
				SetValue(VerificationCodeProperty, value);
				SetValue(VerificationCodeValidProperty, this.IsVerificationCode(value));
			}
		}

		/// <summary>
		/// See <see cref="VerificationCodeValid"/>
		/// </summary>
		public static readonly BindableProperty VerificationCodeValidProperty =
			BindableProperty.Create("VerificationCodeValid", typeof(bool), typeof(ValidatePhoneNrViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool VerificationCodeValid
		{
			get { return (bool)GetValue(VerificationCodeValidProperty); }
			set { SetValue(VerificationCodeValidProperty, value); }
		}

		/// <summary>
		/// The command to bind to for sending a code to the provided phone number.
		/// </summary>
		public ICommand SendCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending a code verification request.
		/// </summary>
		public ICommand VerifyCodeCommand { get; }

		#endregion

		private async Task SendCode()
		{
			if (!this.networkService.IsOnline)
			{
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			this.SetIsBusy(SendCodeCommand);

			try
			{
				object Result = await InternetContent.PostAsync(new Uri("https://id.tagroot.io/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", this.PhoneNumber }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status &&
					Status)
				{
					this.VerificationCode = string.Empty;
					this.CodeSent = true;
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
			finally
			{
				this.BeginInvokeSetIsDone(SendCodeCommand);
			}
		}

		private bool SendCodeCanExecute()
		{
			if (IsBusy) // is connecting
				return false;

			return this.IsInternationalPhoneNumberFormat(this.PhoneNumber);
		}

		private async Task VerifyCode()
		{
			if (!this.networkService.IsOnline)
			{
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			this.SetIsBusy(VerifyCodeCommand);

			try
			{
				object Result = await InternetContent.PostAsync(new Uri("https://id.tagroot.io/ID/VerifyNumber.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", this.PhoneNumber },
						{ "Code", int.Parse(this.VerificationCode) },
						{ "Test", this.Purpose == (int)PurposeUse.EducationalOrExperimental }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status && Status &&
					Response.TryGetValue("Domain", out Obj) && Obj is string Domain &&
					Response.TryGetValue("Key", out Obj) && Obj is string Key &&
					Response.TryGetValue("Secret", out Obj) && Obj is string Secret)
				{
					bool DefaultConnectivity;

					this.TagProfile.SetPhone(this.PhoneNumber);

					try
					{
						(string HostName, int PortNumber, bool IsIpAddress) = await this.networkService.LookupXmppHostnameAndPort(Domain);
						DefaultConnectivity = HostName == Domain && PortNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
					}
					catch (Exception)
					{
						DefaultConnectivity = false;
					}

					UiDispatcher.BeginInvokeOnMainThread(() =>
					{
						this.SetIsDone(VerifyCodeCommand);

						this.TagProfile.SetDomain(Domain, DefaultConnectivity, Key, Secret);
						this.OnStepCompleted(EventArgs.Empty);
					});
				}
				else
				{
					this.CodeSent = false;
					this.VerificationCode = string.Empty;

					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToVerifyCode, AppResources.Ok);
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
			finally
			{
				this.BeginInvokeSetIsDone(VerifyCodeCommand);
			}
		}

		private bool VerifyCodeCanExecute()
		{
			if (IsBusy) // is connecting
				return false;

			return this.IsInternationalPhoneNumberFormat(this.PhoneNumber);
		}

		private bool IsInternationalPhoneNumberFormat(string PhoneNr)
		{
			return !string.IsNullOrEmpty(PhoneNr) && internationalPhoneNr.IsMatch(PhoneNr);
		}

		private bool IsVerificationCode(string Code)
		{
			return !string.IsNullOrEmpty(Code) && verificationCode.IsMatch(Code);
		}

		private static readonly Regex internationalPhoneNr = new Regex(@"^\+[1-9]\d{4,}$", RegexOptions.Singleline);
		private static readonly Regex verificationCode = new Regex(@"^[1-9]\d{5}$", RegexOptions.Singleline);
	}
}