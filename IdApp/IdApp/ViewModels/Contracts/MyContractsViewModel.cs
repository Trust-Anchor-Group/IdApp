using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IdApp.Navigation.Contracts;
using IdApp.ViewModels.Contracts.ObjectModel;
using IdApp.Views.Contracts;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
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
		internal readonly Dictionary<string, Contract> contractsMap;

		/// <summary>
		/// Show created contracts or signed contracts?
		/// </summary>
		internal readonly ContractsListMode contractsListMode;

		private readonly INeuronService neuronService;
		private readonly INetworkService networkService;
		internal readonly INavigationService navigationService;
		internal readonly IUiDispatcher uiDispatcher;
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
			this.neuronService = neuronService ?? Types.Instantiate<INeuronService>(false);
			this.networkService = networkService ?? Types.Instantiate<INetworkService>(false);
			this.navigationService = navigationService ?? Types.Instantiate<INavigationService>(false);
			this.uiDispatcher = uiDispatcher ?? Types.Instantiate<IUiDispatcher>(false);
			this.contractsListMode = ContractsListMode;
			this.contractsMap = new Dictionary<string, Contract>();
			this.Categories = new ObservableCollection<ContractCategoryModel>();
			this.IsBusy = true;

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
			this.IsBusy = true;
			this.ShowContractsMissing = false;
			this.loadContractsTimestamp = DateTime.UtcNow;

			await base.DoBind();

			uiDispatcher.BeginInvokeOnMainThread(async () => await LoadContracts(this.loadContractsTimestamp));
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.ShowContractsMissing = false;
			this.loadContractsTimestamp = DateTime.UtcNow;
			this.Categories.Clear();
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
		/// Holds the list of contracts to display, ordered by category.
		/// </summary>
		public ObservableCollection<ContractCategoryModel> Categories { get; }

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
					if (viewModel.contractsListMode == ContractsListMode.ContractTemplates)
						viewModel.uiDispatcher.BeginInvokeOnMainThread(async () => await viewModel.navigationService.GoToAsync(nameof(NewContractPage), new NewContractNavigationArgs(contract)));
					else
						viewModel.uiDispatcher.BeginInvokeOnMainThread(async () => await viewModel.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(contract, false)));
				}
			});

		/// <summary>
		/// The currently selected contract, if any.
		/// </summary>
		public ContractModel SelectedContract
		{
			get { return (ContractModel)this.GetValue(SelectedContractProperty); }
			set { this.SetValue(SelectedContractProperty, value); }
		}

		private async Task LoadContracts(DateTime now)
		{
			try
			{
				bool succeeded;
				string[] contractIds;
				KeyValuePair<DateTime, string>[] timestampsAndcontractIds;

				switch (this.contractsListMode)
				{
					case ContractsListMode.MyContracts:
						(succeeded, contractIds) = await this.networkService.TryRequest(this.neuronService.Contracts.GetCreatedContractIds);
						timestampsAndcontractIds = AnnotateWithMinDateTime(contractIds);
						break;

					case ContractsListMode.SignedContracts:
						(succeeded, contractIds) = await this.networkService.TryRequest(this.neuronService.Contracts.GetSignedContractIds);
						timestampsAndcontractIds = AnnotateWithMinDateTime(contractIds);
						break;

					case ContractsListMode.ContractTemplates:
						(succeeded, timestampsAndcontractIds) = await this.networkService.TryRequest(this.neuronService.Contracts.GetContractTemplateIds);
						break;

					default:
						return;
				}

				if (!succeeded)
					return;

				if (this.loadContractsTimestamp > now)
					return;

				if (timestampsAndcontractIds.Length <= 0)
				{
					this.uiDispatcher.BeginInvokeOnMainThread(() => this.ShowContractsMissing = true);
					return;
				}

				List<ContractCategoryModel> Categories = new List<ContractCategoryModel>();
				List<ContractModel> Contracts = new List<ContractModel>();
				string LastCategory = null;

				foreach (KeyValuePair<DateTime, string> P in timestampsAndcontractIds)
				{
					DateTime Timestamp = P.Key;
					string ContractId = P.Value;
					Contract contract;

					try
					{
						contract = await this.neuronService.Contracts.GetContract(ContractId);
					}
					catch (Waher.Networking.XMPP.StanzaErrors.ItemNotFoundException)
					{
						continue;   // Contract not available for some reason. Ignore, and display the rest.
					}

					if (this.loadContractsTimestamp > now)
						return;

					if (Timestamp == DateTime.MinValue)
						Timestamp = contract.Created;

					this.contractsMap[ContractId] = contract;

					ContractModel Item = new ContractModel(ContractId, Timestamp, contract);
					string Category = Item.Category;

					if (LastCategory is null)
						LastCategory = Category;
					else if (LastCategory != Category)
					{
						Contracts.Sort(new DateTimeDesc());
						Categories.Add(new ContractCategoryModel(LastCategory, Contracts.ToArray()));
						LastCategory = Category;
						Contracts.Clear();
					}

					Contracts.Add(Item);
				}

				if (Contracts.Count > 0)
				{
					Contracts.Sort(new DateTimeDesc());
					Categories.Add(new ContractCategoryModel(LastCategory, Contracts.ToArray()));
				}

				Categories.Sort(new CategoryAsc());

				foreach (ContractCategoryModel Model in Categories)
					this.Categories.Add(Model);
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		private class DateTimeDesc : IComparer<ContractModel>
		{
			public int Compare(ContractModel x, ContractModel y) => y.Timestamp.CompareTo(x.Timestamp);
		}

		private class CategoryAsc : IComparer<ContractCategoryModel>
		{
			public int Compare(ContractCategoryModel x, ContractCategoryModel y) => x.Category.CompareTo(y.Category);
		}

		private static KeyValuePair<DateTime, string>[] AnnotateWithMinDateTime(string[] IDs)
		{
			if (IDs is null)
				return new KeyValuePair<DateTime, string>[0];

			KeyValuePair<DateTime, string>[] Result = new KeyValuePair<DateTime, string>[IDs.Length];
			int i = 0;

			foreach (string ID in IDs)
				Result[i++] = new KeyValuePair<DateTime, string>(DateTime.MinValue, ID);

			return Result;
		}

	}
}