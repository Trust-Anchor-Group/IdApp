﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Waher.Events;
using Waher.Networking.DNS;
using Xamarin.Forms;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.LifeCycle;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Language;
using Waher.Runtime.Settings;
using Xamarin.Forms.Internals;
using XamarinApp.Views.Registration;
using XamarinApp.Views;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using XamarinApp.Views.Contracts;
using Log = Waher.Events.Log;

namespace XamarinApp
{
	public partial class App
	{
		private static App instance = null;

        private readonly IMessageService messageService;
        private readonly IAuthService authService;
        private FilesProvider filesProvider;
        private InternalSink internalSink;

		public App()
		{
			InitializeComponent();
			// Registrations
            ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterType<TagService>().As<ITagService>().SingleInstance();
			builder.RegisterType<MessageService>().As<IMessageService>().SingleInstance();
			builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
			builder.RegisterType<AuthService>().As<IAuthService>().SingleInstance();
            IContainer container = builder.Build();
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

			TagService = DependencyService.Resolve<ITagService>();
            this.messageService = DependencyService.Resolve<IMessageService>();
            this.authService = DependencyService.Resolve<IAuthService>();
            MainPage = new InitPage();
            instance = this;
        }

        public ITagService TagService { get; }

		#region Page Navigation  - will be moved

		public static async Task ShowPage()
		{
			if (instance.TagService.Configuration.Step >= 2)
			{
				await instance.TagService.UpdateXmpp();

				if (instance.TagService.Configuration.Step > 2 && !instance.TagService.LegalIdentityIsValid)
				{
					instance.TagService.DecrementConfigurationStep();
				}

				if (instance.TagService.Configuration.Step > 4 && !instance.TagService.PinIsValid)
				{
                    instance.TagService.DecrementConfigurationStep();
				}
			}

			Page Page;

			switch (instance.TagService.Configuration.Step)
			{
				case 0:
					Page = new OperatorPage();
					break;

				case 1:
					Page = new AccountPage();
					break;

				case 2:
					if (!instance.TagService.LegalIdentityIsValid)
					{
						DateTime Now = DateTime.Now;
						LegalIdentity Created = null;
						LegalIdentity Approved = null;
						bool Changed = false;

						if (await instance.TagService.CheckServices())
						{
							foreach (LegalIdentity Identity in await instance.TagService.GetLegalIdentitiesAsync())
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
                                instance.TagService.Configuration.LegalIdentity = Approved;
								Changed = true;
							}
							else if (!(Created is null))
							{
                                instance.TagService.Configuration.LegalIdentity = Created;
								Changed = true;
							}

							if (Changed)
							{
								instance.TagService.IncrementConfigurationStep();
								Page = new Views.Registration.IdentityPage();
								break;
							}
						}
					}

					Page = new RegisterIdentityPage();
					break;

				case 3:
					Page = new Views.Registration.IdentityPage();
					break;

				case 4:
					Page = new DefinePinPage();
					break;

				case 5:
				default:
					Page = new MainMenuPage();
					break;
			}

			ShowPage(Page, true);
		}

		public static void ShowPage(Page Page, bool DisposeCurrent)
		{
			void SetPage(Page p, bool disposeCurrent)
			{
				Page Prev = instance.MainPage;

				instance.MainPage = Page;

				if (disposeCurrent && Prev is IDisposable Disposable)
					Disposable.Dispose();
			};
			if (Device.IsInvokeRequired)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					SetPage(Page, DisposeCurrent);
				});
			}
			else
			{
				SetPage(Page, DisposeCurrent);
			}
		}

        internal static Page CurrentPage => instance.MainPage;

#endregion

		internal static App Instance => instance;

        private class InternalSink : EventSink
        {
            public InternalSink()
                : base("InternalEventSink")
            {
            }

            public override Task Queue(Event _)
            {
                return Task.CompletedTask;
            }
        }

		private async Task Setup()
        {
            this.internalSink = new InternalSink();
            Log.Register(this.internalSink);

            try
            {
                Types.Initialize(
                    typeof(App).Assembly,
                    typeof(Database).Assembly,
                    typeof(FilesProvider).Assembly,
                    typeof(ObjectSerializer).Assembly,
                    typeof(XmppClient).Assembly,
                    typeof(ContractsClient).Assembly,
                    typeof(RuntimeSettings).Assembly,
                    typeof(Language).Assembly,
                    typeof(DnsResolver).Assembly,
                    typeof(XmppServerlessMessaging).Assembly,
                    typeof(ProvisioningClient).Assembly);
                await Types.StartAllModules((int)TimeSpan.FromMilliseconds(1000).TotalMilliseconds);
            }
            catch (Exception e)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitleText, e.ToString(), AppResources.OkButtonText);
                return;
            }

            try
            {
                string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string DataFolder = Path.Combine(AppDataFolder, "Data");
				if (filesProvider == null)
                {
                    filesProvider = await FilesProvider.CreateAsync(DataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, 10000, this.authService.GetCustomKey);
                }
                await filesProvider.RepairIfInproperShutdown(string.Empty);
                Database.Register(filesProvider, false);
            }
            catch (Exception e)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitleText, e.ToString(), AppResources.OkButtonText);
            }

            await TagService.Load();
        }

		private async Task Teardown()
        {
            await TagService.Unload();

			await DatabaseModule.Flush();
            await Types.StopAllModules();
            this.filesProvider.Dispose();
            this.filesProvider = null;
            Log.Unregister(this.internalSink);
            Log.Terminate();
            this.internalSink.Dispose();
            this.internalSink = null;
        }

		protected override async void OnStart()
        {
            await Setup();
        }

        protected override async void OnResume()
        {
            await Setup();
        }

		protected override async void OnSleep()
        {
            if (MainPage.BindingContext is BaseViewModel vm)
            {
                await vm.SaveState();
                await vm.Unbind();
            }

            await Teardown();
        }

		public static async Task OpenLegalIdentity(string LegalId, string Purpose)
		{
			try
			{
				LegalIdentity Identity = await instance.TagService.Contracts.GetLegalIdentityAsync(LegalId);
				App.ShowPage(new Views.IdentityPage(instance.MainPage, Identity), true);
			}
			catch (Exception)
			{
				await instance.TagService.Contracts.PetitionIdentityAsync(LegalId, Guid.NewGuid().ToString(), Purpose);
				await instance.MainPage.DisplayAlert("Petition Sent", "A petition has been sent to the owner of the identity. " +
					"If the owner accepts the petition, the identity information will be displayed on the screen.", AppResources.OkButtonText);
			}
		}

		public static async Task OpenContract(string ContractId, string Purpose)
		{
			try
			{
				Contract Contract = await instance.TagService.Contracts.GetContractAsync(ContractId);

				if (Contract.CanActAsTemplate && Contract.State == ContractState.Approved)
					App.ShowPage(new NewContractPage(instance.MainPage, Contract), true);
				else
					App.ShowPage(new ViewContractPage(instance.MainPage, Contract, false), true);
			}
			catch (Exception)
			{
				await instance.TagService.Contracts.PetitionContractAsync(ContractId, Guid.NewGuid().ToString(), Purpose);
				await instance.MainPage.DisplayAlert("Petition Sent", "A petition has been sent to the parts of the contract. " +
					"If any of the parts accepts the petition, the contract information will be displayed on the screen.", AppResources.OkButtonText);
			}
		}
	}
}
