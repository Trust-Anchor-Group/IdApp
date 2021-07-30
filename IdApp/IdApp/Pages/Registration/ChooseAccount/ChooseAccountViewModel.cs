using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Registration.ChooseAccount
{
	/// <summary>
	/// The view model to bind to when showing Step 2 of the registration flow: creating or connecting to an account.
	/// </summary>
	public class ChooseAccountViewModel : RegistrationStepViewModel
	{
		private delegate Task<bool> ConnectMethod();

		private readonly ICryptoService cryptoService;
		private readonly INetworkService networkService;

		/// <summary>
		/// Creates a new instance of the <see cref="ChooseAccountViewModel"/> class.
		/// </summary>
		/// <param name="tagProfile">The tag profile to work with.</param>
		/// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="navigationService">The navigation service to use for app navigation</param>
		/// <param name="settingsService">The settings service for persisting UI state.</param>
		/// <param name="cryptoService">The crypto service to use for password generation.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="logService">The log service.</param>
		public ChooseAccountViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			ISettingsService settingsService,
			ICryptoService cryptoService,
			INetworkService networkService,
			ILogService logService)
			: base(RegistrationStep.Account, tagProfile, uiDispatcher, neuronService, navigationService, settingsService, logService)
		{
			this.cryptoService = cryptoService;
			this.networkService = networkService;
			this.CreateNewCommand = new Command(async _ => await PerformAction(this.CreateAccount, true), _ => CanCreateAccount());
			this.ScanQrCodeCommand = new Command(async _ => await PerformAction(this.ScanQrCode, false), _ => CanScanQrCode());
			this.Title = AppResources.ChooseAccount;
		}

		/// <inheritdoc />
		protected override async Task DoBind()
		{
			await base.DoBind();

			this.DomainName = this.TagProfile.Domain;
			this.TagProfile.Changed += TagProfile_Changed;
		}

		/// <inheritdoc />
		protected override async Task DoUnbind()
		{
			this.TagProfile.Changed -= TagProfile_Changed;
			
			await base.DoUnbind();
		}

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			UiDispatcher.BeginInvokeOnMainThread(() =>
			{
				DomainName = this.TagProfile.Domain;
			});
		}

		#region Properties

		/// <summary>
		/// See <see cref="DomainName"/>
		/// </summary>
		public static readonly BindableProperty DomainNameProperty =
			BindableProperty.Create("DomainName", typeof(string), typeof(ChooseAccountViewModel), default(string));

		/// <summary>
		/// The localized intro text to display to the user for explaining what 'choose account' is for.
		/// </summary>
		public string DomainName
		{
			get { return (string)GetValue(DomainNameProperty); }
			set { SetValue(DomainNameProperty, value); }
		}

		/// <summary>
		/// See <see cref="AccountName"/>
		/// </summary>
		public static readonly BindableProperty AccountNameProperty =
			BindableProperty.Create("AccountName", typeof(string), typeof(ChooseAccountViewModel), default(string));

		/// <summary>
		/// The account name to use when creating a new account.
		/// </summary>
		public string AccountName
		{
			get { return (string)GetValue(AccountNameProperty); }
			set
			{
				this.SetValue(AccountNameProperty, value);
				this.EvaluateCommands(this.CreateNewCommand);
			}
		}

		/// <summary>
		/// Gets or sets the hashed password value.
		/// </summary>
		public string PasswordHash { get; set; }

		/// <summary>
		/// Gets or sets the hash method used when hashing a password.
		/// </summary>
		public string PasswordHashMethod { get; set; }

		/// <summary>
		/// The legal identity, if any. Typically set after creating an account or connecting to an existing account.
		/// </summary>
		public LegalIdentity LegalIdentity { get; set; }

		/// <summary>
		/// The command to bind to for creating a new account.
		/// </summary>
		public ICommand CreateNewCommand { get; }

		/// <summary>
		/// The command to bind to for scanning an invitation or transfer code.
		/// </summary>
		public ICommand ScanQrCodeCommand { get; }

		#endregion

		/// <inheritdoc />
		public override void ClearStepState()
		{
			this.AccountName = string.Empty;
			this.SettingsService.RemoveState(GetSettingsKey(nameof(AccountName)));
		}

		private async Task PerformAction(ConnectMethod Method, bool MakeBusy)
		{
			if (!this.networkService.IsOnline)
			{
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			if (MakeBusy)
				SetIsBusy(CreateNewCommand, ScanQrCodeCommand);

			try
			{
				bool succeeded = await Method();

				UiDispatcher.BeginInvokeOnMainThread(() =>
				{
					if (MakeBusy)
						SetIsDone(CreateNewCommand, ScanQrCodeCommand);

					if (succeeded)
						OnStepCompleted(EventArgs.Empty);
				});
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(ex);
			}
			finally
			{
				if (MakeBusy)
					BeginInvokeSetIsDone(CreateNewCommand, ScanQrCodeCommand);
			}
		}

		private bool CanCreateAccount()
		{
			if (!this.networkService.IsOnline)
				return false;

			if (this.IsBusy)
				return false;

			if (string.IsNullOrWhiteSpace(this.AccountName))
				return false;

			return true;
		}

		private async Task<bool> CreateAccount()
		{
			try
			{
				string passwordToUse = this.cryptoService.CreateRandomPassword();

				(string hostName, int portNumber, bool isIpAddress) = await this.networkService.LookupXmppHostnameAndPort(this.TagProfile.Domain);

				async Task OnConnected(XmppClient client)
				{
					this.PasswordHash = client.PasswordHash;
					this.PasswordHashMethod = client.PasswordHashMethod;

					if (this.TagProfile.NeedsUpdating())
						await this.NeuronService.DiscoverServices(client);

					this.TagProfile.SetAccount(this.AccountName, client.PasswordHash, client.PasswordHashMethod);
				}

				(bool succeeded, string errorMessage) = await this.NeuronService.TryConnectAndCreateAccount(this.TagProfile.Domain,
					isIpAddress, hostName, portNumber, this.AccountName, passwordToUse, Constants.LanguageCodes.Default, 
					this.TagProfile.ApiKey, this.TagProfile.ApiSecret, typeof(App).Assembly, OnConnected);

				if (succeeded)
				{
#if DEBUG
					await Clipboard.SetTextAsync("Password: " + passwordToUse);
					await this.UiDispatcher.DisplayAlert(AppResources.Password, string.Format(AppResources.ThePasswordForTheConnectionIs, passwordToUse), AppResources.Ok);
					System.Diagnostics.Debug.WriteLine("Username: " + this.AccountName);
					System.Diagnostics.Debug.WriteLine("Password: " + passwordToUse);
#endif
					return true;
				}

				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				string userMessage = string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain);
				string message = $"{userMessage}{Environment.NewLine}({ex.Message})";
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, message, AppResources.Ok);
			}

			return false;
		}

		private bool CanScanQrCode()
		{
			if (!this.networkService.IsOnline)
				return false;

			if (this.IsBusy)
				return false;

			return true;
		}

		private async Task<bool> ScanQrCode()
		{
			string URI = await QrCode.ScanQrCode(this.NavigationService, AppResources.ClaimInvitation);
			string Scheme = Constants.UriSchemes.GetScheme(URI);

			if (string.Compare(Scheme, Constants.UriSchemes.UriSchemeTagId, true) != 0)
			{
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.NotAnInvitationCode, AppResources.Ok);
				return false;
			}

			List<KeyValuePair<string, byte[]>> PrivateKeys = null;
			string Data = Constants.UriSchemes.GetCode(URI);
			string Name, Value;
			string Domain = null;
			string ApiKey = null;
			string ApiSecret = null;
			string Account = null;
			string Password = null;
			string IdRef = null;
			string Algorithm = null;
			byte[] PrivateKey = null;
			int i;

			foreach (string Part in Data.Split('&'))
			{
				i = Part.IndexOf('=');
				if (i < 0)
					continue;

				Name = Part.Substring(0, i);
				Value = Part.Substring(i + 1);

				switch (Name.ToLower())
				{
					case "d": Domain = Value; break;
					case "k": ApiKey = Value; break;
					case "s": ApiSecret = Value; break;
					case "a": Account = Value; break;
					case "w": Password = Value; break;
					case "i": IdRef = Value; break;

					case "g":
						Algorithm = Value;
						if (PrivateKey is null)
							break;

						if (PrivateKeys is null)
							PrivateKeys = new List<KeyValuePair<string, byte[]>>();

						PrivateKeys.Add(new KeyValuePair<string, byte[]>(Algorithm, PrivateKey));

						Algorithm = null;
						PrivateKey = null;
						break;

					case "p":
						try
						{
							PrivateKey = Convert.FromBase64String(Value);
						}
						catch (Exception)
						{
							await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.InvalidInvitationCode, AppResources.Ok);
							return false;
						}

						if (Algorithm is null)
							break;

						if (PrivateKeys is null)
							PrivateKeys = new List<KeyValuePair<string, byte[]>>();

						PrivateKeys.Add(new KeyValuePair<string, byte[]>(Algorithm, PrivateKey));

						Algorithm = null;
						PrivateKey = null;
						break;
				}
			}

			if (!string.IsNullOrEmpty(Domain))
			{
				SetIsBusy(CreateNewCommand, ScanQrCodeCommand);
				try
				{
					bool succeeded = true;
					bool DefaultConnectivity;

					try
					{
						(string HostName, int PortNumber, bool IsIpAddress) = await this.networkService.LookupXmppHostnameAndPort(Domain);
						DefaultConnectivity = HostName == Domain && PortNumber == XmppCredentials.DefaultPort;
					}
					catch (Exception)
					{
						DefaultConnectivity = false;
					}

					this.TagProfile.SetDomain(Domain, DefaultConnectivity, ApiKey, ApiSecret);

					if (!string.IsNullOrEmpty(Account) && !string.IsNullOrEmpty(Password))
					{
						this.AccountName = AccountName;
						if (!await this.ConnectToAccount(Password, IdRef, PrivateKeys?.ToArray()))
							succeeded = false;
					}

					UiDispatcher.BeginInvokeOnMainThread(() =>
					{
						SetIsDone(CreateNewCommand, ScanQrCodeCommand);

						if (succeeded)
							OnStepCompleted(EventArgs.Empty);
					});
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					await this.UiDispatcher.DisplayAlert(ex);
				}
				finally
				{
					BeginInvokeSetIsDone(CreateNewCommand, ScanQrCodeCommand);
				}
			}

			return true;
		}

		private async Task<bool> ConnectToAccount(string Password, string LegalIdentityJid, KeyValuePair<string, byte[]>[] PrivateKeys)
		{
			try
			{
				async Task OnConnected(XmppClient client)
				{
					this.PasswordHash = client.PasswordHash;
					this.PasswordHashMethod = client.PasswordHashMethod;

					DateTime now = DateTime.Now;
					LegalIdentity createdIdentity = null;
					LegalIdentity approvedIdentity = null;

					bool serviceDiscoverySucceeded;

					if (this.TagProfile.NeedsUpdating())
						serviceDiscoverySucceeded = await this.NeuronService.DiscoverServices(client);
					else
						serviceDiscoverySucceeded = true;

					if (serviceDiscoverySucceeded)
					{
						if (!(PrivateKeys is null))
							await this.NeuronService.Contracts.ContractsClient.ImportKeys(PrivateKeys);

						foreach (LegalIdentity identity in await this.NeuronService.Contracts.GetLegalIdentities(client))
						{
							if ((string.IsNullOrEmpty(LegalIdentityJid) || string.Compare(LegalIdentityJid, identity.Id, true) == 0) &&
								identity.HasClientSignature &&
								identity.HasClientPublicKey &&
								identity.From <= now &&
								identity.To >= now &&
								(identity.State == IdentityState.Approved || identity.State == IdentityState.Created) &&
								identity.ValidateClientSignature() &&
								await this.NeuronService.Contracts.HasPrivateKey(identity.Id, client))
							{
								if (identity.State == IdentityState.Approved)
								{
									approvedIdentity = identity;
									break;
								}

								if (createdIdentity is null)
									createdIdentity = identity;
							}
						}

						if (!(approvedIdentity is null))
							this.LegalIdentity = approvedIdentity;
						else if (!(createdIdentity is null))
							this.LegalIdentity = createdIdentity;

						if (!(this.LegalIdentity is null))
							this.TagProfile.SetAccountAndLegalIdentity(this.AccountName, client.PasswordHash, client.PasswordHashMethod, this.LegalIdentity);
						else
							this.TagProfile.SetAccount(this.AccountName, client.PasswordHash, client.PasswordHashMethod);
					}
				}

				(string hostName, int portNumber, bool isIpAddress) = await this.networkService.LookupXmppHostnameAndPort(this.TagProfile.Domain);

				(bool succeeded, string errorMessage) = await this.NeuronService.TryConnectAndConnectToAccount(this.TagProfile.Domain,
					isIpAddress, hostName, portNumber, this.AccountName, Password, Constants.LanguageCodes.Default,
					typeof(App).Assembly, OnConnected);

				if (!succeeded)
					await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);

				return succeeded;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain), AppResources.Ok);
			}

			return false;
		}

		/// <inheritdoc />
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();
			await this.SettingsService.SaveState(GetSettingsKey(nameof(AccountName)), this.AccountName);
		}

		/// <inheritdoc />
		protected override async Task DoRestoreState()
		{
			this.AccountName = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(AccountName)));
			await base.DoRestoreState();
		}
	}
}