using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Waher.IoTGateway.Setup;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Security;

namespace XamarinApp.Connection
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(true)]
	public partial class AccountPage : ContentPage, IBackButton
	{
		private readonly XmppConfiguration xmppConfiguration;

		public AccountPage(XmppConfiguration XmppConfiguration)
		{
			this.xmppConfiguration = XmppConfiguration;
			InitializeComponent();
			this.BindingContext = this;
			this.Introduction.Text = this.Introduction.Text.Replace("{Binding Domain}", XmppConfiguration.Domain);
		}

		private async void BackButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				if (this.CreateNewButton.IsVisible)
				{
					if (this.xmppConfiguration.Step > 0)
					{
						this.xmppConfiguration.Step--;
						await Database.Update(this.xmppConfiguration);
					}

					await App.ShowPage();
				}
				else
				{
					this.CreateNewButton.IsVisible = true;
					this.UseExistingButton.IsVisible = true;
					this.AccountNameLabel.IsVisible = false;
					this.AccountName.IsVisible = false;
					this.SwitchTable.IsVisible = false;
					this.PasswordLabel.IsVisible = false;
					this.Password.IsVisible = false;
					this.RetypePasswordLabel.IsVisible = false;
					this.RetypePassword.IsVisible = false;
					this.CreateButton.IsVisible = false;
					this.ConnectButton.IsVisible = false;
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		public string Domain
		{
			get => this.xmppConfiguration.Domain;
		}

		public string Account
		{
			get => this.xmppConfiguration.Account;
		}

		private void CreateNewButton_Clicked(object sender, EventArgs e)
		{
			this.CreateNewButton.IsVisible = false;
			this.UseExistingButton.IsVisible = false;
			this.AccountNameLabel.IsVisible = true;
			this.AccountName.IsVisible = true;
			this.CreateButton.IsVisible = true;
			this.SwitchTable.IsVisible = true;
			this.SwitchTable.HeightRequest = this.BackButton.Height;
			this.RandomPassword_OnChanged(sender, new ToggledEventArgs(this.RandomPassword.On));
			this.AccountName.Focus();
		}

		private void UseExistingButton_Clicked(object sender, EventArgs e)
		{
			this.CreateNewButton.IsVisible = false;
			this.UseExistingButton.IsVisible = false;
			this.AccountNameLabel.IsVisible = true;
			this.AccountName.IsVisible = true;
			this.PasswordLabel.IsVisible = true;
			this.Password.IsVisible = true;
			this.RetypePasswordLabel.IsVisible = false;
			this.RetypePassword.IsVisible = false;
			this.SwitchTable.IsVisible = false;
			this.ConnectButton.IsVisible = true;
			this.AccountName.Focus();
		}

		private void AccountName_Completed(object sender, EventArgs e)
		{
			if (this.Password.IsVisible)
				this.Password.Focus();
		}

		private void Password_Completed(object sender, EventArgs e)
		{
			if (this.RetypePassword.IsVisible)
				this.RetypePassword.Focus();
			else
				this.ScrollView.ScrollToAsync(this.BackButton, ScrollToPosition.End, false);
		}

		private void RetypePassword_Completed(object sender, EventArgs e)
		{
			this.ScrollView.ScrollToAsync(this.BackButton, ScrollToPosition.End, false);
		}

		private void RandomPassword_OnChanged(object sender, ToggledEventArgs e)
		{
			bool ManualPassword = !this.RandomPassword.On;
			this.Password.IsVisible = ManualPassword;
			this.PasswordLabel.IsVisible = ManualPassword;
			this.RetypePassword.IsVisible = ManualPassword;
			this.RetypePasswordLabel.IsVisible = ManualPassword;

			if (ManualPassword)
				this.Password.Focus();
		}

		private async void ConnectButton_Clicked(object sender, EventArgs e)
		{
			this.ConnectButton.IsEnabled = false;
			this.AccountName.IsEnabled = false;
			this.Password.IsEnabled = false;
			this.Connecting.IsVisible = true;
			this.Connecting.IsRunning = true;

			InMemorySniffer Sniffer = new InMemorySniffer();
			bool Success = false;

			try
			{
				(string HostName, int PortNumber) = await OperatorPage.GetXmppClientService(this.xmppConfiguration.Domain);

				using (XmppClient Client = new XmppClient(HostName, PortNumber,
					this.AccountName.Text, this.Password.Text, string.Empty, typeof(App).Assembly, Sniffer))
				{
					TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>();
					bool StreamNegotiation = false;
					bool StreamOpened = false;
					bool StartingEncryption = false;
					bool Authentication = false;
					bool Timeout = false;

					Client.TrustServer = false;
					Client.AllowCramMD5 = false;
					Client.AllowDigestMD5 = false;
					Client.AllowPlain = false;
					Client.AllowEncryption = true;
					Client.AllowScramSHA1 = true;

					Client.OnStateChanged += (sender2, NewState) =>
					{
						switch (NewState)
						{
							case XmppState.StreamNegotiation:
								StreamNegotiation = true;
								break;

							case XmppState.StreamOpened:
								StreamOpened = true;
								break;

							case XmppState.StartingEncryption:
								StartingEncryption = true;
								break;

							case XmppState.Authenticating:
								Authentication = true;
								break;

							case XmppState.Connected:
								Result.TrySetResult(true);
								break;

							case XmppState.Offline:
							case XmppState.Error:
								Result.TrySetResult(false);
								break;
						}

						return Task.CompletedTask;
					};

					Client.Connect(this.xmppConfiguration.Domain);

					using (Timer Timer = new Timer((P) =>
					{
						Timeout = true;
						Result.TrySetResult(false);
					}, null, 10000, 0))
					{
						Success = await Result.Task;
					}

					if (Success)
					{
						this.xmppConfiguration.Account = this.AccountName.Text;
						this.xmppConfiguration.PasswordHash = Client.PasswordHash;
						this.xmppConfiguration.PasswordHashMethod = Client.PasswordHashMethod;

						if (this.xmppConfiguration.Step == 1)
							this.xmppConfiguration.Step++;

						await Database.Update(this.xmppConfiguration);

						if (this.xmppConfiguration.LegalIdentity is null ||
							this.xmppConfiguration.LegalIdentity.State == IdentityState.Compromised ||
							this.xmppConfiguration.LegalIdentity.State == IdentityState.Obsoleted ||
							this.xmppConfiguration.LegalIdentity.State == IdentityState.Rejected)
						{
							DateTime Now = DateTime.Now;
							LegalIdentity Created = null;
							LegalIdentity Approved = null;
							bool Changed = false;

							if (!string.IsNullOrEmpty(this.xmppConfiguration.LegalJid) ||
								await App.FindServices(Client))
							{
								using (ContractsClient Contracts = await ContractsClient.Create(Client, this.xmppConfiguration.LegalJid))
								{
									foreach (LegalIdentity Identity in await Contracts.GetLegalIdentitiesAsync())
									{
										if (Identity.HasClientSignature &&
											Identity.HasClientPublicKey &&
											Identity.From <= Now &&
											Identity.To >= Now &&
											(Identity.State == IdentityState.Approved || Identity.State == IdentityState.Created) &&
											Identity.ValidateClientSignature())
										{
											if (Identity.State == IdentityState.Approved)
											{
												Approved = Identity;
												break;
											}
											else if (Created is null)
												Created = Identity;
										}
									}

									if (!(Approved is null))
									{
										this.xmppConfiguration.LegalIdentity = Approved;
										Changed = true;
									}
									else if (!(Created is null))
									{
										this.xmppConfiguration.LegalIdentity = Created;
										Changed = true;
									}

									if (Changed)
									{
										if (this.xmppConfiguration.Step == 2)
											this.xmppConfiguration.Step++;

										await Database.Update(this.xmppConfiguration);
									}
								}
							}
						}
					}
					else
					{
						if (!StreamNegotiation || Timeout)
							await this.DisplayAlert("Error", "Cannot connect to " + this.xmppConfiguration.Domain, "OK");
						else if (!StreamOpened)
							await this.DisplayAlert("Error", this.xmppConfiguration.Domain + " is not a valid operator.", "OK");
						else if (!StartingEncryption)
							await this.DisplayAlert("Error", this.xmppConfiguration.Domain + " does not follow the ubiquitous encryption policy.", "OK");
						else if (!Authentication)
							await this.DisplayAlert("Error", "Unable to authentication with " + this.xmppConfiguration.Domain + ".", "OK");
						else
							await this.DisplayAlert("Error", "Invalid user name or password.", "OK");
					}
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", "Unable to connect to " + this.xmppConfiguration.Domain + ":\r\n\r\n" + ex.Message, "OK");
			}
			finally
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					this.ConnectButton.IsEnabled = true;
					this.AccountName.IsEnabled = true;
					this.Password.IsEnabled = true;
					this.Connecting.IsVisible = false;
					this.Connecting.IsRunning = false;

					if (Success)
					{
						Task _ = App.ShowPage();
					}
				});
			}
		}

		private async void CreateButton_Clicked(object sender, EventArgs e)
		{
			string Password;

			if (this.RandomPassword.On)
				Password = Hashes.BinaryToString(App.GetBytes(16));
			else if ((Password = this.Password.Text) != this.RetypePassword.Text)
			{
				await this.DisplayAlert("Error", "Passwords do not match.", "OK");
				return;
			}

			this.CreateButton.IsEnabled = false;
			this.AccountName.IsEnabled = false;
			this.Password.IsEnabled = false;
			this.Connecting.IsVisible = true;
			this.Connecting.IsRunning = true;

			InMemorySniffer Sniffer = new InMemorySniffer();

			try
			{
				(string HostName, int PortNumber) = await OperatorPage.GetXmppClientService(this.xmppConfiguration.Domain);

				using (XmppClient Client = new XmppClient(HostName, PortNumber,
					this.AccountName.Text, Password, string.Empty, typeof(App).Assembly, Sniffer))
				{
					TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>();
					bool StreamNegotiation = false;
					bool StreamOpened = false;
					bool StartingEncryption = false;
					bool Authentication = false;
					bool Registering = false;
					bool Timeout = false;

					if (XmppConfiguration.TryGetKeys(this.xmppConfiguration.Domain, out string Key, out string Secret))
						Client.AllowRegistration(Key, Secret);
					else
						Client.AllowRegistration();

					Client.TrustServer = false;
					Client.AllowCramMD5 = false;
					Client.AllowDigestMD5 = false;
					Client.AllowPlain = false;
					Client.AllowEncryption = true;
					Client.AllowScramSHA1 = true;

					Client.OnStateChanged += (sender2, NewState) =>
					{
						switch (NewState)
						{
							case XmppState.StreamNegotiation:
								StreamNegotiation = true;
								break;

							case XmppState.StreamOpened:
								StreamOpened = true;
								break;

							case XmppState.StartingEncryption:
								StartingEncryption = true;
								break;

							case XmppState.Authenticating:
								Authentication = true;
								break;

							case XmppState.Registering:
								Registering = true;
								break;

							case XmppState.Connected:
								Result.TrySetResult(true);
								break;

							case XmppState.Offline:
							case XmppState.Error:
								Result.TrySetResult(false);
								break;
						}

						return Task.CompletedTask;
					};

					Client.Connect(this.xmppConfiguration.Domain);

					bool Success;

					using (Timer Timer = new Timer((P) =>
					{
						Timeout = true;
						Result.TrySetResult(false);
					}, null, 10000, 0))
					{
						Success = await Result.Task;
					}

					if (Success)
					{
						this.xmppConfiguration.Account = this.AccountName.Text;
						this.xmppConfiguration.PasswordHash = Client.PasswordHash;
						this.xmppConfiguration.PasswordHashMethod = Client.PasswordHashMethod;

						if (this.xmppConfiguration.Step == 1)
							this.xmppConfiguration.Step++;

						await Database.Update(this.xmppConfiguration);

						if (this.RandomPassword.On)
							await this.DisplayAlert("Password", "The password for the connection is " + Password, "OK");

						await App.ShowPage();
					}
					else
					{
						if (!StreamNegotiation || Timeout)
							await this.DisplayAlert("Error", "Cannot connect to " + this.xmppConfiguration.Domain, "OK");
						else if (!StreamOpened)
							await this.DisplayAlert("Error", this.xmppConfiguration.Domain + " is not a valid operator.", "OK");
						else if (!StartingEncryption)
							await this.DisplayAlert("Error", this.xmppConfiguration.Domain + " does not follow the ubiquitous encryption policy.", "OK");
						else if (!Authentication)
							await this.DisplayAlert("Error", "Unable to authentication with " + this.xmppConfiguration.Domain + ".", "OK");
						else if (!Registering)
							await this.DisplayAlert("Error", "The operator " + this.xmppConfiguration.Domain + " does not support registration of new accounts.", "OK");
						else
							await this.DisplayAlert("Error", "Account name already taken. Choose another.", "OK");
					}
				}
			}
			catch (Exception)
			{
				await this.DisplayAlert("Error", "Unable to connect to " + this.xmppConfiguration.Domain, "OK");
			}
			finally
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					this.CreateButton.IsEnabled = true;
					this.AccountName.IsEnabled = true;
					this.Password.IsEnabled = true;
					this.Connecting.IsVisible = false;
					this.Connecting.IsRunning = false;
				});
			}
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}

	}
}
