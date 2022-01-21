using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IdApp.Pages.Contracts.MyContracts.ObjectModel;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Contracts.ViewContract;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Pages.Contracts.MyContracts
{
	/// <summary>
	/// The view model to bind to when displaying 'my' contracts.
	/// </summary>
	public class MyContractsViewModel : BaseViewModel
	{
		internal readonly Dictionary<string, Contract> contractsMap;

		internal ContractsListMode contractsListMode;
		private DateTime loadContractsTimestamp;
		private TaskCompletionSource<Contract> selection;

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsViewModel"/> class.
		/// </summary>
		protected internal MyContractsViewModel()
		{
			this.contractsMap = new Dictionary<string, Contract>();
			this.Categories = new ObservableCollection<ContractCategoryModel>();
			this.IsBusy = true;
			this.Action = SelectContractAction.ViewContract;
			this.selection = null;
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			this.IsBusy = true;
			this.ShowContractsMissing = false;
			this.loadContractsTimestamp = DateTime.UtcNow;

			if (this.NavigationService.TryPopArgs(out MyContractsNavigationArgs args))
			{
				this.contractsListMode = args.Mode;
				this.Action = args.Action;
				this.selection = args.Selection;

				switch (this.contractsListMode)
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

			await base.DoBind();

			this.UiSerializer.BeginInvokeOnMainThread(async () => await LoadContracts(this.loadContractsTimestamp));
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			if (this.Action != SelectContractAction.Select)
			{
				this.ShowContractsMissing = false;
				this.loadContractsTimestamp = DateTime.UtcNow;
				this.Categories.Clear();
				this.contractsMap.Clear();
			}

			this.selection?.TrySetResult(null);

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
		/// <see cref="Action"/>
		/// </summary>
		public static readonly BindableProperty ActionProperty =
			BindableProperty.Create("Action", typeof(SelectContractAction), typeof(MyContractsViewModel), default(SelectContractAction));

		/// <summary>
		/// The action to take when contact has been selected.
		/// </summary>
		public SelectContractAction Action
		{
			get { return (SelectContractAction)GetValue(ActionProperty); }
			set { SetValue(ActionProperty, value); }
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
			BindableProperty.Create("SelectedContract", typeof(ContractModel), typeof(MyContractsViewModel), default(ContractModel),
				propertyChanged: async (b, oldValue, newValue) =>
				{
					MyContractsViewModel viewModel = (MyContractsViewModel)b;
					ContractModel model = (ContractModel)newValue;

					if (!(model is null) && viewModel.contractsMap.TryGetValue(model.ContractId, out Contract Contract))
					{
						switch (viewModel.Action)
						{
							case SelectContractAction.ViewContract:
								if (viewModel.contractsListMode == ContractsListMode.ContractTemplates)
								{
									viewModel.UiSerializer.BeginInvokeOnMainThread(async () => await viewModel.NavigationService.GoToAsync(
										nameof(NewContractPage), new NewContractNavigationArgs(Contract)));
								}
								else
								{
									viewModel.UiSerializer.BeginInvokeOnMainThread(async () => await viewModel.NavigationService.GoToAsync(
										nameof(ViewContractPage), new ViewContractNavigationArgs(Contract, false)));
								}
								break;

							case SelectContractAction.Select:
								viewModel.selection?.TrySetResult(Contract);
								await viewModel.NavigationService.GoBackAsync();
								break;
						}
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
						(succeeded, contractIds) = await this.NetworkService.TryRequest(this.NeuronService.Contracts.GetCreatedContractIds);
						timestampsAndcontractIds = AnnotateWithMinDateTime(contractIds);
						break;

					case ContractsListMode.SignedContracts:
						(succeeded, contractIds) = await this.NetworkService.TryRequest(this.NeuronService.Contracts.GetSignedContractIds);
						timestampsAndcontractIds = AnnotateWithMinDateTime(contractIds);
						break;

					case ContractsListMode.ContractTemplates:
						(succeeded, timestampsAndcontractIds) = await this.NetworkService.TryRequest(this.NeuronService.Contracts.GetContractTemplateIds);
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
					this.UiSerializer.BeginInvokeOnMainThread(() => this.ShowContractsMissing = true);
					return;
				}

				SortedDictionary<string, List<ContractModel>> ContractsByCategory = new SortedDictionary<string, List<ContractModel>>(StringComparer.CurrentCultureIgnoreCase);

				foreach (KeyValuePair<DateTime, string> P in timestampsAndcontractIds)
				{
					DateTime Timestamp = P.Key;
					string ContractId = P.Value;
					Contract contract;

					try
					{
						contract = await this.NeuronService.Contracts.GetContract(ContractId);
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

					ContractModel Item = await ContractModel.Create(ContractId, Timestamp, contract, this.TagProfile, this.NeuronService);
					string Category = Item.Category;

					if (!ContractsByCategory.TryGetValue(Category, out List<ContractModel> Contracts))
					{
						Contracts = new List<ContractModel>();
						ContractsByCategory[Category] = Contracts;
					}

					Contracts.Add(Item);
				}

				List<ContractCategoryModel> Categories = new List<ContractCategoryModel>();

				foreach (KeyValuePair<string, List<ContractModel>> P in ContractsByCategory)
				{
					P.Value.Sort(new DateTimeDesc());
					Categories.Add(new ContractCategoryModel(P.Key, P.Value.ToArray()));
				}

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