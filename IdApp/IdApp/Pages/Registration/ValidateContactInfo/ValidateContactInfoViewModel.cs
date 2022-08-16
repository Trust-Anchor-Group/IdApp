using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Xamarin.Forms;
using IdApp.Services.Tag;
using IdApp.Resx;

namespace IdApp.Pages.Registration.ValidateContactInfo
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
	public class ValidateContactInfoViewModel : RegistrationStepViewModel
	{

		private int countSeconds = 30;
		/// <summary>
		/// Creates a new instance of the <see cref="ValidateContactInfoViewModel"/> class.
		/// </summary>
		public ValidateContactInfoViewModel()
			: base(RegistrationStep.ValidateContactInfo)
		{
			this.SendEMailCodeCommand = new Command(async () => await this.SendEMailCode(), this.SendEMailCodeCanExecute);
			this.VerifyEMailCodeCommand = new Command(async () => await this.VerifyEMailCode(), this.VerifyEMailCodeCanExecute);

			this.SendPhoneNrCodeCommand = new Command(async () => await this.SendPhoneNrCode(), this.SendPhoneNrCodeCanExecute);
			this.VerifyPhoneNrCodeCommand = new Command(async () => await this.VerifyPhoneNrCode(), this.VerifyPhoneNrCodeCanExecute);

			this.Title = AppResources.ContactInformation;
			this.Purposes = new ObservableCollection<string>();
			this.EmailButtonLabel = AppResources.SendCode;
			this.PhoneButtonLabel = AppResources.SendCode;
			this.VerifyEmailCodeButtonLabel = AppResources.VerifyCode;
			this.VerifyPhoneCodeButtonLabel = AppResources.VerifyCode;
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
						new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"), string.Empty,
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

			this.EMail = this.TagProfile.EMail;

			this.EvaluateAllCommands();
		}

		/// <inheritdoc/>
		protected override void OnStepCompleted(EventArgs e)
		{
			if (this.PhoneNrValidated && this.EMailValidated)
			{
				base.OnStepCompleted(e);
			}
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(
				this.SendPhoneNrCodeCommand, this.VerifyPhoneNrCodeCommand,
				this.SendEMailCodeCommand, this.VerifyEMailCodeCommand);
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
			BindableProperty.Create(nameof(Purpose), typeof(int), typeof(ValidateContactInfoViewModel), -1);

		/// <summary>
		/// Phone number
		/// </summary>
		public int Purpose
		{
			get => (int)this.GetValue(PurposeProperty);
			set
			{
				this.SetValue(PurposeProperty, value);
				this.PurposeSelected = true;
			}
		}

		/// <summary>
		/// See <see cref="PurposeSelected"/>
		/// </summary>
		public static readonly BindableProperty PurposeSelectedProperty =
			BindableProperty.Create(nameof(PurposeSelected), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool PurposeSelected
		{
			get => (bool)this.GetValue(PurposeSelectedProperty);
			set => this.SetValue(PurposeSelectedProperty, value);
		}

		/// <summary>
		/// See <see cref="EMail"/>
		/// </summary>
		public static readonly BindableProperty EMailProperty =
			BindableProperty.Create(nameof(EMail), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// e-mail
		/// </summary>
		public string EMail
		{
			get => (string)this.GetValue(EMailProperty);
			set
			{
				this.SetValue(EMailProperty, value);
				this.SetValue(EMailValidProperty, this.IsEMailAdress(value));
				this.SetValue(EmailButtonDisabledDisabledProperty, this.IsEMailAdress(value));
				this.SetValue(VerifyEmailCodeButtonLabelProperty, AppResources.VerificationCode);
			}
		}

		/// <summary>
		/// See <see cref="EmailButtonLabel"/>
		/// </summary>
		public static readonly BindableProperty EmailLabelProperty =
			BindableProperty.Create(nameof(EmailButtonLabel), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// The label of the SendEMailCodeButton
		/// </summary>
		public string EmailButtonLabel
		{
			get => (string)this.GetValue(EmailLabelProperty);
			set
			{
				this.SetValue(EmailLabelProperty, value);
			}
		}

		/// <summary>
		/// See <see cref="VerifyEmailCodeButtonLabel"/>
		/// </summary>
		public static readonly BindableProperty VerifyEmailCodeButtonLabelProperty =
			BindableProperty.Create(nameof(VerifyEmailCodeButtonLabel), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		public string VerifyEmailCodeButtonLabel
		{
			get => (string)this.GetValue(VerifyEmailCodeButtonLabelProperty);
			set
			{
				this.SetValue(VerifyEmailCodeButtonLabelProperty, value);
			}
		}

		/// <summary>
		/// See <see cref="EMailValid"/>
		/// </summary>
		public static readonly BindableProperty EMailValidProperty =
			BindableProperty.Create(nameof(EMailValid), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If e-mail address is valid or not
		/// </summary>
		public bool EMailValid
		{
			get => (bool)this.GetValue(EMailValidProperty);
			set => this.SetValue(EMailValidProperty, value);
		}

		/// <summary>
		/// See <see cref="EmailButtonDisabled"/>
		/// </summary>
		public static readonly BindableProperty EmailButtonDisabledDisabledProperty =
			BindableProperty.Create(nameof(EmailButtonDisabled), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If send e-mail code button is disabled or not
		/// </summary>
		public bool EmailButtonDisabled
		{
			get => (bool)this.GetValue(EmailButtonDisabledDisabledProperty);
			set => this.SetValue(EmailButtonDisabledDisabledProperty, value);
		}

		/// <summary>
		/// See <see cref="EMailCodeSent"/>
		/// </summary>
		public static readonly BindableProperty EMailCodeSentProperty =
			BindableProperty.Create(nameof(EMailCodeSent), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool EMailCodeSent
		{
			get => (bool)this.GetValue(EMailCodeSentProperty);
			set => this.SetValue(EMailCodeSentProperty, value);
		}

		/// <summary>
		/// See <see cref="EMailVerificationCode"/>
		/// </summary>
		public static readonly BindableProperty EMailVerificationCodeProperty =
			BindableProperty.Create(nameof(EMailVerificationCode), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// Phone number
		/// </summary>
		public string EMailVerificationCode
		{
			get => (string)this.GetValue(EMailVerificationCodeProperty);
			set
			{
				this.SetValue(EMailVerificationCodeProperty, value);
				this.SetValue(EMailVerificationCodeValidProperty, this.IsVerificationCode(value));
			}
		}

		/// <summary>
		/// See <see cref="EMailVerificationCodeValid"/>
		/// </summary>
		public static readonly BindableProperty EMailVerificationCodeValidProperty =
			BindableProperty.Create(nameof(EMailVerificationCodeValid), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool EMailVerificationCodeValid
		{
			get => (bool)this.GetValue(EMailVerificationCodeValidProperty);
			set => this.SetValue(EMailVerificationCodeValidProperty, value);
		}

		/// <summary>
		/// See <see cref="EMailValidated"/>
		/// </summary>
		public static readonly BindableProperty EMailValidatedProperty =
			BindableProperty.Create(nameof(EMailValidated), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool EMailValidated
		{
			get => (bool)this.GetValue(EMailValidatedProperty);
			set => this.SetValue(EMailValidatedProperty, value);
		}

		/// <summary>
		/// See <see cref="PhoneNumber"/>
		/// </summary>
		public static readonly BindableProperty PhoneNumberProperty =
			BindableProperty.Create(nameof(PhoneNumber), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// Phone number
		/// </summary>
		public string PhoneNumber
		{
			get => (string)this.GetValue(PhoneNumberProperty);
			set
			{
				this.SetValue(PhoneNumberProperty, value);
				this.SetValue(PhoneNumberValidProperty, this.IsInternationalPhoneNumberFormat(value));
				this.SetValue(PhoneButtonDisabledProperty, this.IsInternationalPhoneNumberFormat(value));
				this.SetValue(VerifyPhoneCodeButtonLabelProperty, AppResources.VerifyCode);
			}
		}

		/// <summary>
		/// See <see cref="PhoneButtonLabel"/>
		/// </summary>
		public static readonly BindableProperty PhoneNumberLabelProperty =
			BindableProperty.Create(nameof(PhoneButtonLabel), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// The label of the SendPhoneNrCodeButton
		/// </summary>
		public string PhoneButtonLabel
		{
			get => (string)this.GetValue(PhoneNumberLabelProperty);
			set
			{
				this.SetValue(PhoneNumberLabelProperty, value);
			}
		}

		/// <summary>
		/// See <see cref="VerifyPhoneCodeButtonLabel"/>
		/// </summary>
		public static readonly BindableProperty VerifyPhoneCodeButtonLabelProperty =
			BindableProperty.Create(nameof(VerifyPhoneCodeButtonLabel), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		public string VerifyPhoneCodeButtonLabel
		{
			get => (string)this.GetValue(VerifyPhoneCodeButtonLabelProperty);
			set
			{
				this.SetValue(VerifyPhoneCodeButtonLabelProperty, value);
			}
		}

		/// <summary>
		/// See <see cref="PhoneNumberValid"/>
		/// </summary>
		public static readonly BindableProperty PhoneNumberValidProperty =
			BindableProperty.Create(nameof(PhoneNumberValid), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool PhoneNumberValid
		{
			get => (bool)this.GetValue(PhoneNumberValidProperty);
			set => this.SetValue(PhoneNumberValidProperty, value);
		}

		/// <summary>
		/// See <see cref="PhoneButtonDisabled"/>
		/// </summary>
		public static readonly BindableProperty PhoneButtonDisabledProperty =
			BindableProperty.Create(nameof(PhoneButtonDisabled), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If send code Phone button is disabled or not
		/// </summary>
		public bool PhoneButtonDisabled
		{
			get => (bool)this.GetValue(PhoneButtonDisabledProperty);
			set => this.SetValue(PhoneButtonDisabledProperty, value);
		}

		/// <summary>
		/// See <see cref="PhoneNrCodeSent"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrCodeSentProperty =
			BindableProperty.Create(nameof(PhoneNrCodeSent), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool PhoneNrCodeSent
		{
			get => (bool)this.GetValue(PhoneNrCodeSentProperty);
			set => this.SetValue(PhoneNrCodeSentProperty, value);
		}

		/// <summary>
		/// See <see cref="PhoneNrVerificationCode"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrVerificationCodeProperty =
			BindableProperty.Create(nameof(PhoneNrVerificationCode), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// Phone number
		/// </summary>
		public string PhoneNrVerificationCode
		{
			get => (string)this.GetValue(PhoneNrVerificationCodeProperty);
			set
			{
				this.SetValue(PhoneNrVerificationCodeProperty, value);
				this.SetValue(PhoneNrVerificationCodeValidProperty, this.IsVerificationCode(value));
			}
		}

		/// <summary>
		/// See <see cref="PhoneNrVerificationCodeValid"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrVerificationCodeValidProperty =
			BindableProperty.Create(nameof(PhoneNrVerificationCodeValid), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool PhoneNrVerificationCodeValid
		{
			get => (bool)this.GetValue(PhoneNrVerificationCodeValidProperty);
			set => this.SetValue(PhoneNrVerificationCodeValidProperty, value);
		}

		/// <summary>
		/// See <see cref="PhoneNrValidated"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrValidatedProperty =
			BindableProperty.Create(nameof(PhoneNrValidated), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool PhoneNrValidated
		{
			get => (bool)this.GetValue(PhoneNrValidatedProperty);
			set => this.SetValue(PhoneNrValidatedProperty, value);
		}

		/// <summary>
		/// The command to bind to for sending a code to the provided e-mail address.
		/// </summary>
		public ICommand SendEMailCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending an e-mail code verification request.
		/// </summary>
		public ICommand VerifyEMailCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending a code to the provided phone number.
		/// </summary>
		public ICommand SendPhoneNrCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending a phone message code verification request.
		/// </summary>
		public ICommand VerifyPhoneNrCodeCommand { get; }

		#endregion

		#region Commands

		#region Phone Numbers

		private async Task SendPhoneNrCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			this.SetIsBusy(this.SendPhoneNrCodeCommand);
			try
			{
				string TrimmedNumber = this.TrimPhoneNumber(this.PhoneNumber);

				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", TrimmedNumber }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status &&
					Status)
				{
					this.PhoneNrVerificationCode = string.Empty;
					this.PhoneNrCodeSent = true;
					await this.UiSerializer.DisplayAlert(AppResources.WarningTitle, AppResources.SendPhoneNumberWarning);
					this.StartTimer("phone");
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.SendPhoneNrCodeCommand);
			}
		}

		private bool SendPhoneNrCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.IsInternationalPhoneNumberFormat(this.PhoneNumber);
		}

		private async Task VerifyPhoneNrCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			this.SetIsBusy(this.VerifyPhoneNrCodeCommand);
			try
			{
				string TrimmedNumber = this.TrimPhoneNumber(this.PhoneNumber);
				bool IsTest = this.Purpose == (int)PurposeUse.EducationalOrExperimental;

				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", TrimmedNumber },
						{ "Code", int.Parse(this.PhoneNrVerificationCode) },
						{ "Test", IsTest }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status && Status &&
					Response.TryGetValue("Domain", out Obj) && Obj is string Domain &&
					Response.TryGetValue("Key", out Obj) && Obj is string Key &&
					Response.TryGetValue("Secret", out Obj) && Obj is string Secret &&
					Response.TryGetValue("Temporary", out Obj) && Obj is bool IsTemporary)
				{
					bool DefaultConnectivity;

					this.PhoneNrValidated = true;
					this.VerifyPhoneCodeButtonLabel = AppResources.VerifiedButton;
					this.TagProfile.SetPhone(TrimmedNumber);
					this.TagProfile.SetIsTest(IsTest);
					this.TagProfile.SetTestOtpTimestamp(IsTemporary ? DateTime.Now : null);

					try
					{
						(string HostName, int PortNumber, bool IsIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(Domain);
						DefaultConnectivity = HostName == Domain && PortNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
					}
					catch (Exception)
					{
						DefaultConnectivity = false;
					}

					this.TagProfile.SetDomain(Domain, DefaultConnectivity, Key, Secret);
					this.OnStepCompleted(EventArgs.Empty);
				}
				else
				{
					this.PhoneNrValidated = false;
					this.PhoneNrVerificationCode = string.Empty;
					this.VerifyPhoneCodeButtonLabel = AppResources.VerificationCode;
					

					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToVerifyCode, AppResources.Ok);
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.VerifyPhoneNrCodeCommand);
			}
		}

		private bool VerifyPhoneNrCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.IsInternationalPhoneNumberFormat(this.PhoneNumber);
		}

		#endregion

		#region E-Mail

		private async Task SendEMailCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			this.SetIsBusy(this.SendEMailCodeCommand);
			try
			{
				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "EMail", this.EMail }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status &&
					Status)
				{
					this.EMailVerificationCode = string.Empty;
					this.EMailCodeSent = true;
					await this.UiSerializer.DisplayAlert(AppResources.WarningTitle, AppResources.SendEmailWarning);
					this.StartTimer("email");
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.SendEMailCodeCommand);
			}
		}

		private bool SendEMailCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.IsEMailAdress(this.EMail);
		}

		private async Task VerifyEMailCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			this.SetIsBusy(this.VerifyEMailCodeCommand);
			try
			{
				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
					new Dictionary<string, object>()
					{
						{ "EMail", this.EMail },
						{ "Code", int.Parse(this.EMailVerificationCode) }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status && Status)
				{
					this.EMailValidated = true;
					this.VerifyEmailCodeButtonLabel = AppResources.VerifiedButton;
					this.TagProfile.SetEMail(this.EMail);
					this.OnStepCompleted(EventArgs.Empty);
				}
				else
				{
					this.EMailValidated = false;
					this.EMailVerificationCode = string.Empty;
					this.VerifyEmailCodeButtonLabel = AppResources.VerificationCode;

					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToVerifyCode, AppResources.Ok);
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.VerifyEMailCodeCommand);
			}
		}

		private bool VerifyEMailCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.IsInternationalPhoneNumberFormat(this.PhoneNumber);
		}

		#endregion

		#endregion

		#region Syntax

		private string TrimPhoneNumber(string PhoneNr)
		{
			return PhoneNr.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
		}

		private bool IsInternationalPhoneNumberFormat(string PhoneNr)
		{
			if (string.IsNullOrEmpty(PhoneNr))
				return false;

			string phoneNumber = this.TrimPhoneNumber(PhoneNr);
			return internationalPhoneNr.IsMatch(phoneNumber);
		}

		private bool IsVerificationCode(string Code)
		{
			return !string.IsNullOrEmpty(Code) && verificationCode.IsMatch(Code);
		}


		private bool IsEMailAdress(string EMailAddress)
		{
			if (string.IsNullOrEmpty(EMailAddress))
				return false;

			return emailAddress.IsMatch(EMailAddress);
		}

		private void StartTimer(string type)
		{
			Device.StartTimer(TimeSpan.FromSeconds(1), () => {

				if (this.countSeconds > 0)
				{
					this.countSeconds--;
					if (type == "email")
					{
						this.EmailButtonLabel = string.Format(AppResources.DisabledFor, this.countSeconds);
						this.EmailButtonDisabled = false;
					}
					else
					{
						this.PhoneButtonLabel = string.Format(AppResources.DisabledFor, this.countSeconds);
						this.PhoneButtonDisabled = false;
					}

					return true;
				}
				else
				{
					this.EmailButtonDisabled = this.EMailValid;
					this.PhoneButtonDisabled = this.PhoneNumberValid;
					this.countSeconds = 30;
					this.EmailButtonLabel = AppResources.SendCode;
					this.PhoneButtonLabel = AppResources.SendCode;

					return false;
				}
			});
		}

		private static readonly Regex internationalPhoneNr = new(@"^\+[1-9]\d{4,}$", RegexOptions.Singleline);
		private static readonly Regex verificationCode = new(@"^[1-9]\d{5}$", RegexOptions.Singleline);
		private static readonly Regex emailAddress = new(@"^[\w\d](\w|\d|[_\.-][\w\d])*@(\w|\d|[\.-][\w\d]+)+$", RegexOptions.Singleline);

		#endregion
	}
}
