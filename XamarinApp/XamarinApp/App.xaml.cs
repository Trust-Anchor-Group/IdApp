using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Services;
using XamarinApp.ViewModels;

namespace XamarinApp
{
	public partial class App
	{
		private static App instance = null;
        private static readonly TimeSpan AutoSaveInterval = TimeSpan.FromSeconds(1);
        private Timer autoSaveTimer;
        private readonly IStorageService storageService;
        private readonly IMessageService messageService;
        private readonly TagProfile tagProfile;

		public App()
		{
			InitializeComponent();
			this.tagProfile = new TagProfile();
			// Registrations
            ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterType<TagService>().As<ITagService>().SingleInstance();
			builder.RegisterType<MessageService>().As<IMessageService>().SingleInstance();
			builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
			builder.RegisterType<AuthService>().As<IAuthService>().SingleInstance();
			builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
            builder.RegisterInstance(this.tagProfile).SingleInstance();
            IContainer container = builder.Build();
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

			TagService = DependencyService.Resolve<ITagService>();
            this.storageService = DependencyService.Resolve<IStorageService>();
            this.messageService = DependencyService.Resolve<IMessageService>();
            MainPage = new NavigationPage(new InitPage());
            instance = this;
        }

        public ITagService TagService { get; }

		#region Page Navigation  - will be moved

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

		protected override async void OnStart()
        {
            await Start();
        }

		protected override async void OnResume()
        {
            await Start();
        }

		private async Task Start()
        {
			TagConfiguration configuration = await this.storageService.FindFirstDeleteRest<TagConfiguration>();
            if (configuration != null)
            {
                this.tagProfile.FromConfiguration(configuration);
            }
            else
            {
                await this.storageService.Insert(new TagConfiguration());
            }

            this.autoSaveTimer = new Timer(async _ => await AutoSave(), null, AutoSaveInterval, AutoSaveInterval);
            await this.storageService.Load();
            await this.TagService.Load();
        }

		protected override async void OnSleep()
        {
            this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            this.autoSaveTimer.Dispose();
			await AutoSave();
            if (MainPage.BindingContext is BaseViewModel vm)
            {
                await vm.SaveState();
                await vm.Unbind();
            }

            await TagService.Unload();
            await this.storageService.Unload();
        }

		private async Task AutoSave()
        {
            if (this.tagProfile.IsDirty)
            {
                this.tagProfile.ResetIsDirty();
                await this.storageService.Update(this.tagProfile.ToConfiguration());
            }
        }

		//public static async Task OpenLegalIdentity(string LegalId, string Purpose)
		//{
		//	try
		//	{
		//		LegalIdentity Identity = await instance.TagService.GetLegalIdentityAsync(LegalId);
		//		App.ShowPage(new Views.IdentityPage(instance.MainPage, Identity), true);
		//	}
		//	catch (Exception)
		//	{
		//		await instance.TagService.PetitionIdentityAsync(LegalId, Guid.NewGuid().ToString(), Purpose);
		//		await instance.messageService.DisplayAlert("Petition Sent", "A petition has been sent to the owner of the identity. " +
		//			"If the owner accepts the petition, the identity information will be displayed on the screen.", AppResources.Ok);
		//	}
		//}

		//public static async Task OpenContract(string ContractId, string Purpose)
		//{
		//	try
		//	{
		//		Contract Contract = await instance.TagService.GetContractAsync(ContractId);

		//		if (Contract.CanActAsTemplate && Contract.State == ContractState.Approved)
		//			App.ShowPage(new NewContractPage(instance.MainPage, Contract), true);
		//		else
		//			App.ShowPage(new ViewContractPage(instance.MainPage, Contract, false), true);
		//	}
		//	catch (Exception)
		//	{
		//		await instance.TagService.PetitionContractAsync(ContractId, Guid.NewGuid().ToString(), Purpose);
		//		await instance.MainPage.DisplayAlert("Petition Sent", "A petition has been sent to the parts of the contract. " +
		//			"If any of the parts accepts the petition, the contract information will be displayed on the screen.", AppResources.Ok);
		//	}
		//}
	}
}
