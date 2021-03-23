using System;
using System.Threading.Tasks;
using EDaler.Uris;
using EDaler.Uris.Incomplete;
using IdApp.Navigation;
using IdApp.Views.Wallet;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
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
			{
				this.neuronService.Wallet.BalanceUpdated += Wallet_BalanceUpdated;
				this.EndLoad(true);
			}
			
			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.neuronService.Wallet.BalanceUpdated -= Wallet_BalanceUpdated;
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private Task Wallet_BalanceUpdated(object Sender, EDaler.BalanceEventArgs e)
		{
			this.uiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				if ((e.Balance.Event?.Change ?? 0) > 0)
					await this.navigationService.GoToAsync(nameof(EDalerReceivedPage), new EDalerBalanceNavigationArgs(e.Balance));
				else
					await this.navigationService.GoToAsync(nameof(MyWalletPage));
			});

			return Task.CompletedTask;
		}

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

			if (Parsed.Expires.AddDays(1) < DateTime.UtcNow)
			{
				await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.ExpiredEDalerUri);
				return;
			}

			if (Parsed is EDalerIssuerUri)
			{
				this.uiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					await this.navigationService.GoToAsync(nameof(IssueEDalerPage), new EDalerUriNavigationArgs(Parsed));
				});
			}
			else if (Parsed is EDalerDestroyerUri)
			{
				// TODO
			}
			else if (Parsed is EDalerPaymentUri)
			{
				// TODO
			}
			else if (Parsed is EDalerIncompletePaymeUri)	// Incomplete URI
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