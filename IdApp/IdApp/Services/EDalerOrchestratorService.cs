using IdApp.Navigation;
using IdApp.Views;
using IdApp.Views.Contracts;
using IdApp.Views.Registration;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using EDaler.Uris.Incomplete;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Runtime.Inventory;

namespace IdApp.Services
{
	[Singleton]
	internal class EDalerOrchestratorService : LoadableService, IEDalerOrchestratorService
	{
		private readonly ITagProfile tagProfile;
		private readonly IUiDispatcher uiDispatcher;
		private readonly INeuronService neuronService;
		private readonly INavigationService navigationService;
		private readonly ILogService logService;
		private readonly INetworkService networkService;
		private readonly ISettingsService settingsService;

		public EDalerOrchestratorService(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			ILogService logService,
			INetworkService networkService,
			ISettingsService settingsService)
		{
			this.tagProfile = tagProfile;
			this.uiDispatcher = uiDispatcher;
			this.neuronService = neuronService;
			this.navigationService = navigationService;
			this.logService = logService;
			this.networkService = networkService;
			this.settingsService = settingsService;
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

		/// <summary>
		/// eDaler URI scanned.
		/// </summary>
		/// <param name="uri">eDaler URI.</param>
		public async Task OpenEDalerUri(string uri)
		{
			if (!this.neuronService.Wallet.TryParseEDalerUri(uri, out EDalerUri Parsed))
			{
				await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.InvalidEDalerUri);
				return;
			}

			if (Parsed is EDalerIssuerUri IssuerUri)
			{
				// TODO
			}
			else if (Parsed is EDalerDestroyerUri DestroyerUri)
			{
				// TODO
			}
			else if (Parsed is EDalerPaymentUri PaymentUri)
			{
				// TODO
			}
			else if (Parsed is EDalerIncompletePaymeUri PaymeUri)	// Incomplete URI
			{
				// TODO
			}
			else
			{
				await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.UnrecognizedEDalerURI);
				return;
			}
		}

	}
}