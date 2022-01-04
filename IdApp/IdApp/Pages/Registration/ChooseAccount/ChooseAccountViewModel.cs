using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using IdApp.Services.Crypto;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Settings;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using IdApp.Services.UI.QR;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
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
		/// <param name="uiSerializer">The UI dispatcher for alerts.</param>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="navigationService">The navigation service to use for app navigation</param>
		/// <param name="settingsService">The settings service for persisting UI state.</param>
		/// <param name="cryptoService">The crypto service to use for password generation.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="logService">The log service.</param>
		public ChooseAccountViewModel(
			ITagProfile tagProfile,
			IUiSerializer uiSerializer,
			INeuronService neuronService,
			INavigationService navigationService,
			ISettingsService settingsService,
			ICryptoService cryptoService,
			INetworkService networkService,
			ILogService logService)
			: base(RegistrationStep.Account, tagProfile, uiSerializer, neuronService, navigationService, settingsService, logService)
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
			this.UiSerializer.BeginInvokeOnMainThread(() =>
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
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			if (MakeBusy)
				SetIsBusy(CreateNewCommand, ScanQrCodeCommand);

			try
			{
				bool succeeded = await Method();

				this.UiSerializer.BeginInvokeOnMainThread(() =>
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
				await this.UiSerializer.DisplayAlert(ex);
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
					if (this.TagProfile.NeedsUpdating())
						await this.NeuronService.DiscoverServices(client);

					this.TagProfile.SetAccount(this.AccountName, client.PasswordHash, client.PasswordHashMethod);
				}

				(bool succeeded, string errorMessage) = await this.NeuronService.TryConnectAndCreateAccount(this.TagProfile.Domain,
					isIpAddress, hostName, portNumber, this.AccountName, passwordToUse, Constants.LanguageCodes.Default,
					this.TagProfile.ApiKey, this.TagProfile.ApiSecret, typeof(App).Assembly, OnConnected);

				if (succeeded)
					return true;

				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				string userMessage = string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain);
				string message = $"{userMessage}{Environment.NewLine}({ex.Message})";
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, message, AppResources.Ok);
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

			if (string.Compare(Scheme, Constants.UriSchemes.UriSchemeOnboarding, true) != 0)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.NotAnInvitationCode, AppResources.Ok);
				return false;
			}

			string[] Parts = URI.Split(':');
			if (Parts.Length != 5)
			{
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.InvalidInvitationCode, AppResources.Ok);
				return false;
			}

			string Domain = Parts[1];
			string Code = Parts[2];
			string KeyStr = Parts[3];
			string IVStr = Parts[4];
			string EncryptedStr;
			Uri Uri;

			try
			{
				Uri = new Uri("https://" + Domain + "/Onboarding/GetInfo");
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.InvalidInvitationCode, AppResources.Ok);
				return false;
			}

			SetIsBusy(CreateNewCommand, ScanQrCodeCommand);
			try
			{
				try
				{
					KeyValuePair<byte[], string> P = await InternetContent.PostAsync(Uri, Encoding.ASCII.GetBytes(Code), "text/plain",
						new KeyValuePair<string, string>("Accept", "text/plain"));

					object Decoded = await InternetContent.DecodeAsync(P.Value, P.Key, Uri);

					EncryptedStr = (string)Decoded;
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.UnableToAccessInvitation, AppResources.Ok);
					return false;
				}

				try
				{
					byte[] Key = Convert.FromBase64String(KeyStr);
					byte[] IV = Convert.FromBase64String(IVStr);
					byte[] Encrypted = Convert.FromBase64String(EncryptedStr);
					byte[] Decrypted;

					using (Aes Aes = Aes.Create())
					{
						Aes.BlockSize = 128;
						Aes.KeySize = 256;
						Aes.Mode = CipherMode.CBC;
						Aes.Padding = PaddingMode.PKCS7;

						using (ICryptoTransform Decryptor = Aes.CreateDecryptor(Key, IV))
						{
							Decrypted = Decryptor.TransformFinalBlock(Encrypted, 0, Encrypted.Length);
						}
					}

					string Xml = Encoding.UTF8.GetString(Decrypted);

					XmlDocument Doc = new XmlDocument();
					Doc.LoadXml(Xml);

					if (Doc.DocumentElement is null || Doc.DocumentElement.NamespaceURI != ContractsClient.NamespaceOnboarding)
						throw new Exception("Invalid Invitation XML");

					LinkedList<XmlElement> ToProcess = new LinkedList<XmlElement>();
					ToProcess.AddLast(Doc.DocumentElement);

					bool AccountDone = false;
					XmlElement LegalIdDefinition = null;
					string Pin = null;

					while (!(ToProcess.First is null))
					{
						XmlElement E = ToProcess.First.Value;
						ToProcess.RemoveFirst();

						switch (E.LocalName)
						{
							case "ApiKey":
								KeyStr = XML.Attribute(E, "key");
								string Secret = XML.Attribute(E, "secret");
								Domain = XML.Attribute(E, "domain");

								await this.SelectDomain(Domain, KeyStr, Secret);

								await this.UiSerializer.DisplayAlert(AppResources.InvitationAccepted,
									string.Format(AppResources.InvitedToCreateAccountOnDomain, Domain), AppResources.Ok);
								break;

							case "Account":
								string UserName = XML.Attribute(E, "userName");
								string Password = XML.Attribute(E, "password");
								string PasswordMethod = XML.Attribute(E, "passwordMethod");
								Domain = XML.Attribute(E, "domain");

								string DomainBak = this.TagProfile.Domain;
								bool DefaultConnectivityBak = this.TagProfile.DefaultXmppConnectivity;
								string ApiKeyBak = this.TagProfile.ApiKey;
								string ApiSecretBak = this.TagProfile.ApiSecret;

								await this.SelectDomain(Domain, string.Empty, string.Empty);

								if (!await this.ConnectToAccount(UserName, Password, PasswordMethod, string.Empty, LegalIdDefinition, Pin))
								{
									this.TagProfile.SetDomain(DomainBak, DefaultConnectivityBak, ApiKeyBak, ApiSecretBak);
									throw new Exception("Invalid account.");
								}

								LegalIdDefinition = null;
								this.AccountName = UserName;
								AccountDone = true;
								break;

							case "LegalId":
								LegalIdDefinition = E;
								break;

							case "Pin":
								Pin = XML.Attribute(E, "pin");
								break;

							case "Transfer":
								foreach (XmlNode N in E.ChildNodes)
								{
									if (N is XmlElement E2)
										ToProcess.AddLast(E2);
								}
								break;

							default:
								throw new Exception("Invalid Invitation XML");
						}
					}

					if (!(LegalIdDefinition is null))
						await this.NeuronService.Contracts.ContractsClient.ImportKeys(LegalIdDefinition);

					if (AccountDone)
					{
						this.UiSerializer.BeginInvokeOnMainThread(() =>
						{
							SetIsDone(CreateNewCommand, ScanQrCodeCommand);

							OnStepCompleted(EventArgs.Empty);
						});
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.InvalidInvitationCode, AppResources.Ok);
					return false;
				}
			}
			finally
			{
				BeginInvokeSetIsDone(CreateNewCommand, ScanQrCodeCommand);
			}

			return true;
		}

		private async Task SelectDomain(string Domain, string Key, string Secret)
		{
			bool DefaultConnectivity;

			try
			{
				(string HostName, int PortNumber, bool IsIpAddress) = await this.networkService.LookupXmppHostnameAndPort(Domain);
				DefaultConnectivity = HostName == Domain && PortNumber == XmppCredentials.DefaultPort;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				DefaultConnectivity = false;
			}

			this.TagProfile.SetDomain(Domain, DefaultConnectivity, Key, Secret);
		}

		private async Task<bool> ConnectToAccount(string AccountName, string Password, string PasswordMethod, string LegalIdentityJid, 
			XmlElement LegalIdDefinition, string Pin)
		{
			try
			{
				async Task OnConnected(XmppClient client)
				{
					DateTime now = DateTime.Now;
					LegalIdentity createdIdentity = null;
					LegalIdentity approvedIdentity = null;

					bool serviceDiscoverySucceeded;

					if (this.TagProfile.NeedsUpdating())
						serviceDiscoverySucceeded = await this.NeuronService.DiscoverServices(client);
					else
						serviceDiscoverySucceeded = true;

					if (serviceDiscoverySucceeded && !string.IsNullOrEmpty(this.TagProfile.LegalJid))
					{
						bool DestroyContractsClient = false;

						if (!client.TryGetExtension(typeof(ContractsClient), out IXmppExtension Extension) ||
							!(Extension is ContractsClient ContractsClient))
						{
							ContractsClient = new ContractsClient(client, this.TagProfile.LegalJid);
							DestroyContractsClient = true;
						}

						try
						{
							if (!(LegalIdDefinition is null))
								await ContractsClient.ImportKeys(LegalIdDefinition);

							LegalIdentity[] Identities = await ContractsClient.GetLegalIdentitiesAsync();

							foreach (LegalIdentity Identity in Identities)
							{
								if ((string.IsNullOrEmpty(LegalIdentityJid) || string.Compare(LegalIdentityJid, Identity.Id, true) == 0) &&
									Identity.HasClientSignature &&
									Identity.HasClientPublicKey &&
									Identity.From <= now &&
									Identity.To >= now &&
									(Identity.State == IdentityState.Approved || Identity.State == IdentityState.Created) &&
									Identity.ValidateClientSignature() &&
									await ContractsClient.HasPrivateKey(Identity))
								{
									if (Identity.State == IdentityState.Approved)
									{
										approvedIdentity = Identity;
										break;
									}

									if (createdIdentity is null)
										createdIdentity = Identity;
								}
							}

							if (!(approvedIdentity is null))
								this.LegalIdentity = approvedIdentity;
							else if (!(createdIdentity is null))
								this.LegalIdentity = createdIdentity;

							if (!(this.LegalIdentity is null))
								this.TagProfile.SetAccountAndLegalIdentity(AccountName, client.PasswordHash, client.PasswordHashMethod, this.LegalIdentity);
							else
								this.TagProfile.SetAccount(AccountName, client.PasswordHash, client.PasswordHashMethod);

							if (!string.IsNullOrEmpty(Pin))
								this.TagProfile.SetPin(Pin, !string.IsNullOrEmpty(Pin));
						}
						finally
						{
							if (DestroyContractsClient)
								ContractsClient.Dispose();
						}
					}
				}

				(string hostName, int portNumber, bool isIpAddress) = await this.networkService.LookupXmppHostnameAndPort(this.TagProfile.Domain);

				(bool succeeded, string errorMessage) = await this.NeuronService.TryConnectAndConnectToAccount(this.TagProfile.Domain,
					isIpAddress, hostName, portNumber, AccountName, Password, PasswordMethod, Constants.LanguageCodes.Default,
					typeof(App).Assembly, OnConnected);

				if (!succeeded)
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);

				return succeeded;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.UnableToConnectTo, this.TagProfile.Domain), AppResources.Ok);
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