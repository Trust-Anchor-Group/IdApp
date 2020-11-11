using System;
using System.IO;
using System.Text;
using System.Threading;
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

		public ITagService TagService { get; }

		public App()
		{
			InitializeComponent();
			// Registrations
            ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterType<TagService>().As<ITagService>().SingleInstance();
			builder.RegisterType<MessageService>().As<IMessageService>().SingleInstance();
			builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
            IContainer container = builder.Build();
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

			TagService = DependencyService.Resolve<ITagService>();
            this.messageService = DependencyService.Resolve<IMessageService>();

            MainPage = new InitPage();
            instance = this;
        }

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

		private async Task Init()
        {
            Log.Register(new InternalSink());

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


                FilesProvider Provider = await FilesProvider.CreateAsync(DataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, 10000, this.TagService.GetCustomKey);

                await Provider.RepairIfInproperShutdown(string.Empty);

                Database.Register(Provider);
            }
            catch (Exception e)
            {
                await this.messageService.DisplayAlert(AppResources.ErrorTitleText, e.ToString(), AppResources.OkButtonText);
            }
        }

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

		internal static App Instance => instance;
		internal static Page CurrentPage => instance.MainPage;

        protected override async void OnStart()
        {
            await Init();
            await TagService.Load();
        }

        protected override async void OnSleep()
        {
            if (MainPage.BindingContext is BaseViewModel vm)
            {
                await vm.Unbind();
            }
            await TagService.Unload();
		}

		protected override async void OnResume()
        {
            await TagService.Load();
		}

		internal async Task Stop()
		{
			try
			{
				instance = null;

				await Types.StopAllModules();

                await TagService.Unload();
				TagService.Dispose();

				Log.Terminate();
			}
			finally
			{
				await Waher.Persistence.LifeCycle.DatabaseModule.Flush();

				ICloseApplication CloseApp = DependencyService.Get<ICloseApplication>();
				CloseApp?.CloseApplication();
			}
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

		public static bool Back()
		{
			if (instance.MainPage is IBackButton Page)
				return Page.BackClicked();
			else
				return false;
		}


	}
}
