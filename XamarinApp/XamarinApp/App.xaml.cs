using System;
using System.Threading.Tasks;
using Autofac;
using Xamarin.Forms;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Internals;
using XamarinApp.Connection;
using XamarinApp.MainMenu;
using XamarinApp.Services;
using XamarinApp.Views.Contracts;
using Log = Waher.Events.Log;

namespace XamarinApp
{
	public partial class App
	{
		private static App instance = null;

		public static ITagService TagService { get; }

		static App()
		{
			TagService = new TagService();
		}

		public App()
		{
			InitializeComponent();
			MainPage = new InitPage();
			instance = this;
			// Registrations
            ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterType<TagService>().As<ITagService>().SingleInstance();
            IContainer container = builder.Build();
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

		}

		public static async Task ShowPage()
		{
			if (TagService.Configuration.Step >= 2)
			{
				await TagService.UpdateXmpp();

				if (TagService.Configuration.Step > 2 && !TagService.LegalIdentityIsValid)
				{
					TagService.Configuration.Step = 2;
					TagService.UpdateConfiguration();
				}

				if (TagService.Configuration.Step > 4 && !TagService.PinIsValid)
				{
					TagService.Configuration.Step = 4;
					TagService.UpdateConfiguration();
				}
			}

			Page Page;

			switch (TagService.Configuration.Step)
			{
				case 0:
					Page = new OperatorPage();
					break;

				case 1:
					Page = new AccountPage();
					break;

				case 2:
					if (!TagService.LegalIdentityIsValid)
					{
						DateTime Now = DateTime.Now;
						LegalIdentity Created = null;
						LegalIdentity Approved = null;
						bool Changed = false;

						if (await TagService.CheckServices())
						{
							foreach (LegalIdentity Identity in await TagService.GetLegalIdentitiesAsync())
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
								TagService.Configuration.LegalIdentity = Approved;
								Changed = true;
							}
							else if (!(Created is null))
							{
								TagService.Configuration.LegalIdentity = Created;
								Changed = true;
							}

							if (Changed)
							{
								TagService.Configuration.Step++;
								TagService.UpdateConfiguration();
								Page = new Connection.IdentityPage();
								break;
							}
						}
					}

					Page = new RegisterIdentityPage();
					break;

				case 3:
					Page = new Connection.IdentityPage();
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

		protected override void OnSleep()
		{
			TagService.Xmpp?.SetPresence(Availability.Away);
		}

		protected override void OnResume()
		{
			TagService.Xmpp?.SetPresence(Availability.Online);
		}

		internal async Task Stop()
		{
			try
			{
				instance = null;

				await Types.StopAllModules();

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
				LegalIdentity Identity = await TagService.Contracts.GetLegalIdentityAsync(LegalId);
				App.ShowPage(new MainMenu.IdentityPage(instance.MainPage, Identity), true);
			}
			catch (Exception)
			{
				await TagService.Contracts.PetitionIdentityAsync(LegalId, Guid.NewGuid().ToString(), Purpose);
				await instance.MainPage.DisplayAlert("Petition Sent", "A petition has been sent to the owner of the identity. " +
					"If the owner accepts the petition, the identity information will be displayed on the screen.", "OK");
			}
		}

		public static async Task OpenContract(string ContractId, string Purpose)
		{
			try
			{
				Contract Contract = await TagService.Contracts.GetContractAsync(ContractId);

				if (Contract.CanActAsTemplate && Contract.State == ContractState.Approved)
					App.ShowPage(new NewContractPage(instance.MainPage, Contract), true);
				else
					App.ShowPage(new ViewContractPage(instance.MainPage, Contract, false), true);
			}
			catch (Exception)
			{
				await TagService.Contracts.PetitionContractAsync(ContractId, Guid.NewGuid().ToString(), Purpose);
				await instance.MainPage.DisplayAlert("Petition Sent", "A petition has been sent to the parts of the contract. " +
					"If any of the parts accepts the petition, the contract information will be displayed on the screen.", "OK");
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
