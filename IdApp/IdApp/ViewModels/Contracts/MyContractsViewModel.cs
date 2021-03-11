using IdApp.Models;
using IdApp.Navigation;
using IdApp.Views.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.ViewModels.Contracts
{
	/// <summary>
	/// What list of contracts to display
	/// </summary>
	public enum ContractsListMode
	{
		/// <summary>
		/// Contracts I have created
		/// </summary>
		MyContracts,

		/// <summary>
		/// Contracts I have signed
		/// </summary>
		SignedContracts,

		/// <summary>
		/// Contract templates I have used to create new contracts
		/// </summary>
		ContractTemplates
	}

	/// <summary>
	/// The view model to bind to when displaying 'my' contracts.
	/// </summary>
	public class MyContractsViewModel : BaseViewModel
	{
		private readonly Dictionary<string, Contract> contractsMap;
		/// <summary>
		/// Show created contracts or signed contracts?
		/// </summary>
		private readonly ContractsListMode contractsListMode;

		private readonly INeuronService neuronService;
		private readonly INetworkService networkService;
		private readonly INavigationService navigationService;
		private readonly IUiDispatcher uiDispatcher;
		private DateTime loadContractsTimestamp;

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsViewModel"/> class.
		/// </summary>
		/// <param name="ContractsListMode">What list of contracts to display.</param>
		public MyContractsViewModel(ContractsListMode ContractsListMode)
			: this(ContractsListMode, null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsViewModel"/> class.
		/// For unit tests.
		/// </summary>
		/// <param name="ContractsListMode">What list of contracts to display.</param>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="navigationService">The navigation service.</param>
		/// <param name="uiDispatcher"> The dispatcher to use for alerts and accessing the main thread.</param>
		protected internal MyContractsViewModel(ContractsListMode ContractsListMode, INeuronService neuronService, INetworkService networkService, INavigationService navigationService, IUiDispatcher uiDispatcher)
		{
			this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
			this.networkService = networkService ?? DependencyService.Resolve<INetworkService>();
			this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
			this.uiDispatcher = uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>();
			this.contractsListMode = ContractsListMode;
			this.contractsMap = new Dictionary<string, Contract>();
			this.Contracts = new ObservableCollection<ContractModel>();

			switch (ContractsListMode)
			{
				case ContractsListMode.MyContracts:
					this.Title = AppResources.MyContracts;
					this.Description = AppResources.MyContractsInfoText;
					break;

				case ContractsListMode.SignedContracts:
					this.Title = AppResources.SignedContracts;
					this.Description = AppResources.SignedContractsInfoText;
					break;

				case ContractsListMode.ContractTemplates:
					this.Title = AppResources.ContractTemplates;
					this.Description = AppResources.ContractTemplatesInfoText;
					break;
			}
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();
			this.ShowContractsMissing = false;
			this.loadContractsTimestamp = DateTime.UtcNow;
			_ = LoadContracts(this.loadContractsTimestamp);
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.ShowContractsMissing = false;
			this.loadContractsTimestamp = DateTime.UtcNow;
			this.Contracts.Clear();
			this.contractsMap.Clear();
			await base.DoUnbind();
		}

		/// <summary>
		/// See <see cref="Title"/>
		/// </summary>
		public static readonly BindableProperty TitleProperty =
			BindableProperty.Create("Title", typeof(string), typeof(MyContractsViewModel), default(string));

		/// <summary>
		/// Gets or sets the title for the view displaying contracts.
		/// </summary>
		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		/// <summary>
		/// See <see cref="Description"/>
		/// </summary>
		public static readonly BindableProperty DescriptionProperty =
			BindableProperty.Create("Description", typeof(string), typeof(MyContractsViewModel), default(string));

		/// <summary>
		/// Gets or sets the introductory text for the view displaying contracts.
		/// </summary>
		public string Description
		{
			get { return (string)GetValue(DescriptionProperty); }
			set { SetValue(DescriptionProperty, value); }
		}

		/// <summary>
		/// See <see cref="ShowContractsMissing"/>
		/// </summary>
		public static readonly BindableProperty ShowContractsMissingProperty =
			BindableProperty.Create("ShowContractsMissing", typeof(bool), typeof(MyContractsViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether to show a contracts missing alert or not.
		/// </summary>
		public bool ShowContractsMissing
		{
			get { return (bool)GetValue(ShowContractsMissingProperty); }
			set { SetValue(ShowContractsMissingProperty, value); }
		}

		/// <summary>
		/// Holds the list of contracts to display.
		/// </summary>
		public ObservableCollection<ContractModel> Contracts { get; }

		/// <summary>
		/// See <see cref="SelectedContract"/>
		/// </summary>
		public static readonly BindableProperty SelectedContractProperty =
			BindableProperty.Create("SelectedContract", typeof(ContractModel), typeof(MyContractsViewModel), default(ContractModel), propertyChanged: (b, oldValue, newValue) =>
			{
				MyContractsViewModel viewModel = (MyContractsViewModel)b;
				ContractModel model = (ContractModel)newValue;
				if (!(model is null) && viewModel.contractsMap.TryGetValue(model.ContractId, out Contract contract))
				{
					viewModel.uiDispatcher.BeginInvokeOnMainThread(async () => await viewModel.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(contract, false)));
				}
			});

		/// <summary>
		/// The currently selected contract, if any.
		/// </summary>
		public ContractModel SelectedContract
		{
			get { return (ContractModel)GetValue(SelectedContractProperty); }
			set { SetValue(SelectedContractProperty, value); }
		}

		private async Task LoadContracts(DateTime now)
		{
			bool succeeded;
			string[] contractIds;

			switch (this.contractsListMode)
			{
				case ContractsListMode.MyContracts:
					(succeeded, contractIds) = await this.networkService.TryRequest(this.neuronService.Contracts.GetCreatedContractIds);
					break;

				case ContractsListMode.SignedContracts:
					(succeeded, contractIds) = await this.networkService.TryRequest(this.neuronService.Contracts.GetSignedContractIds);
					break;

				case ContractsListMode.ContractTemplates:
					(succeeded, contractIds) = await this.networkService.TryRequest(this.neuronService.Contracts.GetContractTemplateIds);
					break;

				default:
					return;
			}

			if (!succeeded)
				return;

			if (this.loadContractsTimestamp > now)
				return;

			if (contractIds.Length <= 0)
			{
				this.uiDispatcher.BeginInvokeOnMainThread(() => this.ShowContractsMissing = true);
				return;
			}

			foreach (string contractId in contractIds)
			{
				Contract contract = await this.neuronService.Contracts.GetContract(contractId);
				if (this.loadContractsTimestamp > now)
				{
					return;
				}
				this.contractsMap[contractId] = contract;
				ContractModel model = new ContractModel(contract.ContractId, contract.Created, $"{contract.ContractId} ({contract.ForMachinesNamespace}#{contract.ForMachinesLocalName})");
				this.Contracts.Add(model);
			}
		}
	}
}