using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Waher.IoTGateway.Setup;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Views.Registration
{
	[DesignTimeVisible(true)]
	public partial class AccountPage : IBackButton
    {
        private readonly ITagService tagService;

		public AccountPage()
		{
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.BindingContext = this;
			this.Introduction.Text = this.Introduction.Text.Replace("{Binding Domain}", this.tagService.Configuration.Domain);
        }

		private async void BackButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				if (this.CreateNewButton.IsVisible)
				{
                    if (this.tagService.Configuration.Step > 0)
                    {
                        this.tagService.DecrementConfigurationStep();
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
				await this.DisplayAlert(AppResources.ErrorTitleText, ex.Message, AppResources.OkButtonText);
			}
		}

		public string Domain
		{
			get => this.tagService.Configuration.Domain;
		}

		public string Account
		{
			get => this.tagService.Configuration.Account;
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
				(string HostName, int PortNumber) = await this.tagService.GetXmppHostnameAndPort(this.tagService.Configuration.Domain);

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

					Client.Connect(this.tagService.Configuration.Domain);

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
                        if (this.tagService.Configuration.Step == 1)
                            this.tagService.Configuration.Step++;

                        this.tagService.SetAccount(this.AccountName.Text, Client.PasswordHash, Client.PasswordHashMethod);

                        if (!this.tagService.LegalIdentityIsValid)
						{
							DateTime Now = DateTime.Now;
							LegalIdentity Created = null;
							LegalIdentity Approved = null;
							bool Changed = false;

                            if (!string.IsNullOrEmpty(this.tagService.Configuration.LegalJid) ||
                                await this.tagService.FindServices(Client))
							{
								using (ContractsClient Contracts = await ContractsClient.Create(Client, this.tagService.Configuration.LegalJid))
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
                                        this.tagService.Configuration.LegalIdentity = Approved;
										Changed = true;
									}
									else if (!(Created is null))
									{
                                        this.tagService.Configuration.LegalIdentity = Created;
										Changed = true;
									}

									if (Changed)
									{
                                        if (this.tagService.Configuration.Step == 2)
                                            this.tagService.Configuration.Step++;

                                        this.tagService.UpdateConfiguration();
									}
								}
							}
						}
					}
					else
					{
                        if (!StreamNegotiation || Timeout)
                            await this.DisplayAlert(AppResources.ErrorTitleText, "Cannot connect to " + this.tagService.Configuration.Domain, AppResources.OkButtonText);
                        else if (!StreamOpened)
                            await this.DisplayAlert(AppResources.ErrorTitleText, this.tagService.Configuration.Domain + " is not a valid operator.", AppResources.OkButtonText);
                        else if (!StartingEncryption)
                            await this.DisplayAlert(AppResources.ErrorTitleText, this.tagService.Configuration.Domain + " does not follow the ubiquitous encryption policy.", AppResources.OkButtonText);
                        else if (!Authentication)
                            await this.DisplayAlert(AppResources.ErrorTitleText, "Unable to authentication with " + this.tagService.Configuration.Domain + ".", AppResources.OkButtonText);
                        else
                            await this.DisplayAlert(AppResources.ErrorTitleText, "Invalid user name or password.", AppResources.OkButtonText);
					}
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert(AppResources.ErrorTitleText, $"Unable to connect to {this.tagService.Configuration.Domain}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}{ex.Message}", AppResources.OkButtonText);
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
                Password = this.tagService.CreateRandomPassword();
			else if ((Password = this.Password.Text) != this.RetypePassword.Text)
			{
				await this.DisplayAlert(AppResources.ErrorTitleText, "Passwords do not match.", AppResources.OkButtonText);
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
                (string HostName, int PortNumber) = await this.tagService.GetXmppHostnameAndPort(this.tagService.Configuration.Domain);

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

					if (XmppConfiguration.TryGetKeys(this.tagService.Configuration.Domain, out string Key, out string Secret))
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

					Client.Connect(this.tagService.Configuration.Domain);

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
                        if (this.tagService.Configuration.Step == 1)
                            this.tagService.Configuration.Step++;

						this.tagService.SetAccount(this.AccountName.Text, Client.PasswordHash, Client.PasswordHashMethod);

						if (this.RandomPassword.On)
							await this.DisplayAlert("Password", "The password for the connection is " + Password, AppResources.OkButtonText);

						await App.ShowPage();
					}
					else
					{
                        if (!StreamNegotiation || Timeout)
                            await this.DisplayAlert(AppResources.ErrorTitleText, "Cannot connect to " + this.tagService.Configuration.Domain, AppResources.OkButtonText);
                        else if (!StreamOpened)
                            await this.DisplayAlert(AppResources.ErrorTitleText, this.tagService.Configuration.Domain + " is not a valid operator.", AppResources.OkButtonText);
                        else if (!StartingEncryption)
                            await this.DisplayAlert(AppResources.ErrorTitleText, this.tagService.Configuration.Domain + " does not follow the ubiquitous encryption policy.", AppResources.OkButtonText);
                        else if (!Authentication)
                            await this.DisplayAlert(AppResources.ErrorTitleText, "Unable to authentication with " + this.tagService.Configuration.Domain + ".", AppResources.OkButtonText);
                        else if (!Registering)
                            await this.DisplayAlert(AppResources.ErrorTitleText, "The operator " + this.tagService.Configuration.Domain + " does not support registration of new accounts.", AppResources.OkButtonText);
                        else
                            await this.DisplayAlert(AppResources.ErrorTitleText, "Account name already taken. Choose another.", AppResources.OkButtonText);
					}
				}
			}
			catch (Exception)
			{
				await this.DisplayAlert(AppResources.ErrorTitleText, "Unable to connect to " + this.tagService.Configuration.Domain, AppResources.OkButtonText);
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
			this.BackButton_Clicked(this, EventArgs.Empty);
			return true;
		}

	}
}
