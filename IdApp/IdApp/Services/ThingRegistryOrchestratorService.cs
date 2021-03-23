using System;
using System.Threading.Tasks;
using IdApp.Navigation;
using IdApp.Views.Things;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
	[Singleton]
	internal class ThingRegistryOrchestratorService : LoadableService, IThingRegistryOrchestratorService
	{
		private readonly ITagProfile tagProfile;
		private readonly IUiDispatcher uiDispatcher;
		private readonly INeuronService neuronService;
		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private readonly INetworkService networkService;

		public ThingRegistryOrchestratorService(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			ILogService logService,
			INetworkService networkService)
		{
			this.tagProfile = tagProfile;
			this.uiDispatcher = uiDispatcher;
			this.neuronService = neuronService;
			this.navigationService = navigationService;
			this.logService = logService;
			this.networkService = networkService;
		}

		public override Task Load(bool isResuming)
		{
			if (this.BeginLoad())
				this.EndLoad(true);
			
			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
				this.EndUnload();

			return Task.CompletedTask;
		}

		#region Event Handlers

		#endregion

		public Task OpenClaimDevice(string Uri)
		{
			this.uiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				await this.navigationService.GoToAsync(nameof(ViewClaimThingPage), new ViewClaimThingNavigationArgs(Uri));
			});

			return Task.CompletedTask;
		}

		public Task OpenSearchDevices(string Uri)
		{
			return Task.CompletedTask;
		}

	}
}