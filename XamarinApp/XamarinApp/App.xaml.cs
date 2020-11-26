﻿using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Waher.Events;
using Waher.Networking.DNS;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.Provisioning;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.LifeCycle;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Script;
using Waher.Script.Graphs;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using XamarinApp.Services;
using XamarinApp.ViewModels;
using Log = Waher.Events.Log;

namespace XamarinApp
{
	public partial class App : IDisposable
	{
        private static readonly TimeSpan AutoSaveInterval = TimeSpan.FromSeconds(1);
        private Timer autoSaveTimer;
        private InternalSink internalSink;
        private readonly INeuronService neuronService;
        private readonly IContractsService contractsService;
        private readonly IStorageService storageService;
        private readonly INavigationService navigationService;
        private readonly TagProfile tagProfile;

		public App()
		{
			InitializeComponent();
			this.tagProfile = new TagProfile();
			// Registrations
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(this.tagProfile).SingleInstance();
			builder.RegisterType<NeuronService>().As<INeuronService>().SingleInstance();
			builder.RegisterType<ContractsService>().As<IContractsService>().SingleInstance();
			builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
			builder.RegisterType<StorageService>().As<IStorageService>().SingleInstance();
			builder.RegisterType<AuthService>().As<IAuthService>().SingleInstance();
			builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
            IContainer container = builder.Build();
			// Set AutoFac to be the dependency resolver
            DependencyResolver.ResolveUsing(type => container.IsRegistered(type) ? container.Resolve(type) : null);

			// Resolve what's needed for the App class
			this.neuronService = DependencyService.Resolve<INeuronService>();
			this.contractsService = DependencyService.Resolve<IContractsService>();
            this.storageService = DependencyService.Resolve<IStorageService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.internalSink = new InternalSink();
            Log.Register(this.internalSink);

            Types.Initialize(
                typeof(App).Assembly,
                typeof(Database).Assembly,
                typeof(FilesProvider).Assembly,
                typeof(ObjectSerializer).Assembly,
                typeof(XmppClient).Assembly,
                typeof(ContractsClient).Assembly,
                typeof(Expression).Assembly,
                typeof(Graph).Assembly,
                typeof(Waher.Things.ThingReference).Assembly,
                typeof(RuntimeSettings).Assembly,
                typeof(Waher.Runtime.Language.Language).Assembly,
                typeof(DnsResolver).Assembly,
                typeof(Waher.Networking.XMPP.Sensor.SensorClient).Assembly,
                typeof(Waher.Networking.XMPP.Control.ControlClient).Assembly,
                typeof(Waher.Networking.XMPP.Concentrator.ConcentratorClient).Assembly,
                typeof(XmppServerlessMessaging).Assembly,
                typeof(ProvisioningClient).Assembly,
                typeof(Waher.Security.EllipticCurves.EllipticCurve).Assembly);

			// Start page
            this.MainPage = new NavigationPage(new InitPage());
        }

        public void Dispose()
        {
            DatabaseModule.Flush().GetAwaiter().GetResult();
            Types.StopAllModules().GetAwaiter().GetResult();
            Log.Unregister(this.internalSink);
            Log.Terminate();
            this.internalSink.Dispose();
            this.internalSink = null;
        }

        protected override async void OnStart()
        {
            await PerformStartup();
        }

		protected override async void OnResume()
        {
            await PerformStartup();
        }

        protected override async void OnSleep()
        {
            await PerformShutdown();
        }

		private async Task PerformStartup()
        {
            await this.storageService.Load();
            await this.neuronService.Load();

            TagConfiguration configuration = await this.storageService.FindFirstDeleteRest<TagConfiguration>();
            if (configuration == null)
            {
                configuration = new TagConfiguration();
                await this.storageService.Insert(configuration);
            }
            this.tagProfile.FromConfiguration(configuration);

            this.autoSaveTimer = new Timer(async _ => await AutoSave(), null, AutoSaveInterval, AutoSaveInterval);

            this.contractsService.PetitionForPeerReviewIdReceived += ContractsService_PetitionForPeerReviewIdReceived;
            this.contractsService.PetitionForIdentityReceived += ContractsService_PetitionForIdentityReceived;
            this.contractsService.PetitionedContractResponseReceived += ContractsService_PetitionedContractResponseReceived;
            this.contractsService.PetitionForContractReceived += ContractsService_PetitionForContractReceived;
            this.contractsService.PetitionedIdentityResponseReceived += ContractsService_PetitionedIdentityResponseReceived;
        }

        private async Task PerformShutdown()
        {
            this.contractsService.PetitionForPeerReviewIdReceived -= ContractsService_PetitionForPeerReviewIdReceived;
            this.contractsService.PetitionForIdentityReceived -= ContractsService_PetitionForIdentityReceived;
            this.contractsService.PetitionedContractResponseReceived -= ContractsService_PetitionedContractResponseReceived;
            this.contractsService.PetitionForContractReceived -= ContractsService_PetitionForContractReceived;
            this.contractsService.PetitionedIdentityResponseReceived -= ContractsService_PetitionedIdentityResponseReceived;

            this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            this.autoSaveTimer.Dispose();
            await AutoSave();
            if (MainPage.BindingContext is BaseViewModel vm)
            {
                await vm.SaveState();
                await vm.Unbind();
            }

            await this.neuronService.Unload();
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

        #region Event Handlers

        private void ContractsService_PetitionForPeerReviewIdReceived(object sender, SignaturePetitionEventArgs e)
        {
            if (this.tagProfile.IsCompleteOrWaitingForValidation())
            {
                // TODO: fix navigation
                //await this.navigationService.PushAsync(new IdentityPage(Application.Current.MainPage, e.RequestorIdentity, e))
            }
        }

        private void ContractsService_PetitionForIdentityReceived(object sender, LegalIdentityPetitionEventArgs e)
        {
            if (this.tagProfile.IsCompleteOrWaitingForValidation())
            {
                // TODO: fix navigation
                //await this.navigationService.PushAsync(new PetitionIdentityPage(Application.Current.MainPage, e.RequestorIdentity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose), false);
            }
        }

        private void ContractsService_PetitionedContractResponseReceived(object sender, ContractPetitionResponseEventArgs e)
        {
            if (!e.Response || e.RequestedContract is null)
            {
                this.navigationService.DisplayAlert(AppResources.Message, "Petition to view contract was denied.", AppResources.Ok);
            }
            else
            {
                // TODO: fix navigation
                //await this.navigationService.PushAsync(new ViewContractPage(Application.Current.MainPage, e.RequestedContract, false), false);
            }
        }

        private void ContractsService_PetitionForContractReceived(object sender, ContractPetitionEventArgs e)
        {
            // TODO: fix navigation
            //await this.navigationService.PushAsync(new PetitionContractPage(Application.Current.MainPage, e.RequestorIdentity, e.RequestorFullJid, Contract, e.PetitionId, e.Purpose), false);
        }

        private void ContractsService_PetitionedIdentityResponseReceived(object sender, LegalIdentityPetitionResponseEventArgs e)
        {
            if (!e.Response || e.RequestedIdentity is null)
            {
                this.navigationService.DisplayAlert(AppResources.Message, "Petition to view legal identity was denied.", AppResources.Ok);
            }
            else
            {
                // TODO: fix navigation
                //await this.navigationService.PushAsync(new Views.IdentityPage(Application.Current.MainPage, e.RequestedIdentity), false);
            }
        }

        #endregion

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
