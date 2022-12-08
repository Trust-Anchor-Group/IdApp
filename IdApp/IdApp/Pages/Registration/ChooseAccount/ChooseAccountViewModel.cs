using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using IdApp.Services.Tag;
using IdApp.Services.UI.QR;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Registration.ChooseAccount
{
	/// <summary>
	/// The view model to bind to when showing Step 2 of the registration flow: creating or connecting to an account.
	/// </summary>
	public class ChooseAccountViewModel : RegistrationStepViewModel
	{
		private delegate Task<bool> ConnectMethod();

		/// <summary>
		/// Creates a new instance of the <see cref="ChooseAccountViewModel"/> class.
		/// </summary>
		public ChooseAccountViewModel()
			: base(RegistrationStep.Account)
		{
			this.CreateNewCommand = new Command(async _ => await this.PerformAction(this.CreateAccount, true), _ => this.CanCreateAccount());
			this.ScanQrCodeCommand = new Command(async _ => await this.PerformAction(this.ScanQrCode, false), _ => this.CanScanQrCode());
			this.Title = LocalizationResourceManager.Current["ChooseAccount"];
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.DomainName = this.TagProfile.Domain;
			this.TagProfile.Changed += this.TagProfile_Changed;
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			this.TagProfile.Changed -= this.TagProfile_Changed;

			await base.OnDispose();
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.DomainName = this.TagProfile.Domain;
			});
		}

		#region Properties

		/// <summary>
		/// See <see cref="DomainName"/>
		/// </summary>
		public static readonly BindableProperty DomainNameProperty =
			BindableProperty.Create(nameof(DomainName), typeof(string), typeof(ChooseAccountViewModel), default(string));

		/// <summary>
		/// The localized intro text to display to the user for explaining what 'choose account' is for.
		/// </summary>
		public string DomainName
		{
			get => (string)this.GetValue(DomainNameProperty);
			set => this.SetValue(DomainNameProperty, value);
		}

		/// <summary>
		/// See <see cref="AccountName"/>
		/// </summary>
		public static readonly BindableProperty AccountNameProperty =
			BindableProperty.Create(nameof(AccountName), typeof(string), typeof(ChooseAccountViewModel), default(string));

		/// <summary>
		/// The account name to use when creating a new account.
		/// </summary>
		public string AccountName
		{
			get => (string)this.GetValue(AccountNameProperty);
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
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.AccountName)));
		}

		private async Task PerformAction(ConnectMethod Method, bool MakeBusy)
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			if (MakeBusy)
				this.SetIsBusy(this.CreateNewCommand, this.ScanQrCodeCommand);

			try
			{
				bool succeeded = await Method();

				this.UiSerializer.BeginInvokeOnMainThread(() =>
				{
					if (MakeBusy)
						this.SetIsDone(this.CreateNewCommand, this.ScanQrCodeCommand);

					if (succeeded)
						this.OnStepCompleted(EventArgs.Empty);
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
					this.BeginInvokeSetIsDone(this.CreateNewCommand, this.ScanQrCodeCommand);
			}
		}

		private bool CanCreateAccount()
		{
			if (!this.NetworkService.IsOnline)
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
				string passwordToUse = this.CryptoService.CreateRandomPassword();

				(string hostName, int portNumber, bool isIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(this.TagProfile.Domain);

				async Task OnConnected(XmppClient client)
				{
					if (this.TagProfile.NeedsUpdating())
						await this.XmppService.DiscoverServices(client);

					this.TagProfile.SetAccount(this.AccountName, client.PasswordHash, client.PasswordHashMethod);
				}

				(bool succeeded, string errorMessage) = await this.XmppService.TryConnectAndCreateAccount(this.TagProfile.Domain,
					isIpAddress, hostName, portNumber, this.AccountName, passwordToUse, Constants.LanguageCodes.Default,
					this.TagProfile.ApiKey, this.TagProfile.ApiSecret, typeof(App).Assembly, OnConnected);

				if (succeeded)
					return true;

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], errorMessage, LocalizationResourceManager.Current["Ok"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				string userMessage = string.Format(LocalizationResourceManager.Current["UnableToConnectTo"], this.TagProfile.Domain);
				string message = userMessage + Environment.NewLine + ex.Message;
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], message, LocalizationResourceManager.Current["Ok"]);
			}

			return false;
		}

		private bool CanScanQrCode()
		{
			if (!this.NetworkService.IsOnline)
				return false;

			if (this.IsBusy)
				return false;

			return true;
		}

		private async Task<bool> ScanQrCode()
		{
			string URI = await QrCode.ScanQrCode(LocalizationResourceManager.Current["ClaimInvitation"], UseShellNavigationService: false);
			if (string.IsNullOrEmpty(URI))
				return false;

			string Scheme = Constants.UriSchemes.GetScheme(URI);

			if (string.Compare(Scheme, Constants.UriSchemes.UriSchemeOnboarding, true) != 0)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NotAnInvitationCode"], LocalizationResourceManager.Current["Ok"]);
				return false;
			}

			string[] Parts = URI.Split(':');
			if (Parts.Length != 5)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["InvalidInvitationCode"], LocalizationResourceManager.Current["Ok"]);
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
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["InvalidInvitationCode"], LocalizationResourceManager.Current["Ok"]);
				return false;
			}

			this.SetIsBusy(this.CreateNewCommand, this.ScanQrCodeCommand);
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
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToAccessInvitation"], LocalizationResourceManager.Current["Ok"]);
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

						using ICryptoTransform Decryptor = Aes.CreateDecryptor(Key, IV);
						Decrypted = Decryptor.TransformFinalBlock(Encrypted, 0, Encrypted.Length);
					}

					string Xml = Encoding.UTF8.GetString(Decrypted);

					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(Xml);

					if (Doc.DocumentElement is null || Doc.DocumentElement.NamespaceURI != ContractsClient.NamespaceOnboarding)
						throw new Exception("Invalid Invitation XML");

					LinkedList<XmlElement> ToProcess = new();
					ToProcess.AddLast(Doc.DocumentElement);

					bool AccountDone = false;
					XmlElement LegalIdDefinition = null;
					string Pin = null;

					while (ToProcess.First is not null)
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

								await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InvitationAccepted"],
									string.Format(LocalizationResourceManager.Current["InvitedToCreateAccountOnDomain"], Domain), LocalizationResourceManager.Current["Ok"]);
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

					if (LegalIdDefinition is not null)
						await this.XmppService.Contracts.ContractsClient.ImportKeys(LegalIdDefinition);

					if (AccountDone)
					{
						this.UiSerializer.BeginInvokeOnMainThread(() =>
						{
							this.SetIsDone(this.CreateNewCommand, this.ScanQrCodeCommand);

							this.OnStepCompleted(EventArgs.Empty);
						});
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["InvalidInvitationCode"], LocalizationResourceManager.Current["Ok"]);
					return false;
				}
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.CreateNewCommand, this.ScanQrCodeCommand);
			}

			return true;
		}

		private async Task SelectDomain(string Domain, string Key, string Secret)
		{
			bool DefaultConnectivity;

			try
			{
				(string HostName, int PortNumber, bool IsIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(Domain);
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
						serviceDiscoverySucceeded = await this.XmppService.DiscoverServices(client);
					else
						serviceDiscoverySucceeded = true;

					if (serviceDiscoverySucceeded && !string.IsNullOrEmpty(this.TagProfile.LegalJid))
					{
						bool DestroyContractsClient = false;

						if (!client.TryGetExtension(typeof(ContractsClient), out IXmppExtension Extension) ||
							Extension is not ContractsClient ContractsClient)
						{
							ContractsClient = new ContractsClient(client, this.TagProfile.LegalJid);
							DestroyContractsClient = true;
						}

						try
						{
							if (LegalIdDefinition is not null)
								await ContractsClient.ImportKeys(LegalIdDefinition);

							LegalIdentity[] Identities = await ContractsClient.GetLegalIdentitiesAsync();

							foreach (LegalIdentity Identity in Identities)
							{
								try
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
								catch (Exception)
								{
									// Keys might not be available. Ignore at this point. Keys will be generated later.
								}
							}

							if (approvedIdentity is not null)
								this.LegalIdentity = approvedIdentity;
							else if (createdIdentity is not null)
								this.LegalIdentity = createdIdentity;

							string SelectedId;

							if (this.LegalIdentity is not null)
							{
								this.TagProfile.SetAccountAndLegalIdentity(AccountName, client.PasswordHash, client.PasswordHashMethod, this.LegalIdentity);
								SelectedId = this.LegalIdentity.Id;
							}
							else
							{
								this.TagProfile.SetAccount(AccountName, client.PasswordHash, client.PasswordHashMethod);
								SelectedId = string.Empty;
							}

							if (!string.IsNullOrEmpty(Pin))
								this.TagProfile.CompletePinStep(Pin);

							foreach (LegalIdentity Identity in Identities)
							{
								if (Identity.Id == SelectedId)
									continue;

								switch (Identity.State)
								{
									case IdentityState.Approved:
									case IdentityState.Created:
										await ContractsClient.ObsoleteLegalIdentityAsync(Identity.Id);
										break;
								}
							}
						}
						finally
						{
							if (DestroyContractsClient)
								ContractsClient.Dispose();
						}
					}
				}

				(string hostName, int portNumber, bool isIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(this.TagProfile.Domain);

				(bool succeeded, string errorMessage) = await this.XmppService.TryConnectAndConnectToAccount(this.TagProfile.Domain,
					isIpAddress, hostName, portNumber, AccountName, Password, PasswordMethod, Constants.LanguageCodes.Default,
					typeof(App).Assembly, OnConnected);

				if (!succeeded)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], errorMessage, LocalizationResourceManager.Current["Ok"]);

				return succeeded;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["UnableToConnectTo"], this.TagProfile.Domain), LocalizationResourceManager.Current["Ok"]);
			}

			return false;
		}

		/// <inheritdoc />
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.AccountName)), this.AccountName);
		}

		/// <inheritdoc />
		protected override async Task DoRestoreState()
		{
			this.AccountName = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.AccountName)));
			await base.DoRestoreState();
		}
	}
}
