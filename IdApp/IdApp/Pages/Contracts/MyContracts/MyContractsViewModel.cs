using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IdApp.Pages.Contracts.MyContracts.ObjectModel;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Resx;
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
			BindableProperty.Create(nameof(Title), typeof(string), typeof(MyContractsViewModel), default(string));

		/// <summary>
		/// Gets or sets the title for the view displaying contracts.
		/// </summary>
		public string Title
		{
			get { return (string)this.GetValue(TitleProperty); }
			set { this.SetValue(TitleProperty, value); }
		}

		/// <summary>
		/// See <see cref="Description"/>
		/// </summary>
		public static readonly BindableProperty DescriptionProperty =
			BindableProperty.Create(nameof(Description), typeof(string), typeof(MyContractsViewModel), default(string));

		/// <summary>
		/// Gets or sets the introductory text for the view displaying contracts.
		/// </summary>
		public string Description
		{
			get { return (string)this.GetValue(DescriptionProperty); }
			set { this.SetValue(DescriptionProperty, value); }
		}

		/// <summary>
		/// <see cref="Action"/>
		/// </summary>
		public static readonly BindableProperty ActionProperty =
			BindableProperty.Create(nameof(Action), typeof(SelectContractAction), typeof(MyContractsViewModel), default(SelectContractAction));

		/// <summary>
		/// The action to take when contact has been selected.
		/// </summary>
		public SelectContractAction Action
		{
			get { return (SelectContractAction)this.GetValue(ActionProperty); }
			set { this.SetValue(ActionProperty, value); }
		}

		/// <summary>
		/// See <see cref="ShowContractsMissing"/>
		/// </summary>
		public static readonly BindableProperty ShowContractsMissingProperty =
			BindableProperty.Create(nameof(ShowContractsMissing), typeof(bool), typeof(MyContractsViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether to show a contracts missing alert or not.
		/// </summary>
		public bool ShowContractsMissing
		{
			get { return (bool)this.GetValue(ShowContractsMissingProperty); }
			set { this.SetValue(ShowContractsMissingProperty, value); }
		}

		/// <summary>
		/// Holds the list of contracts to display, ordered by category.
		/// </summary>
		public ObservableCollection<ContractCategoryModel> Categories { get; }

		/// <summary>
		/// See <see cref="SelectedContract"/>
		/// </summary>
		public static readonly BindableProperty SelectedContractProperty =
			BindableProperty.Create(nameof(SelectedContract), typeof(ContractModel), typeof(MyContractsViewModel), default(ContractModel),
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
				bool Succeeded;
				Contract[] Contracts;
				ContractReference[] ContractReferences;

				switch (this.contractsListMode)
				{
					case ContractsListMode.MyContracts:
						(Succeeded, Contracts) = await this.NetworkService.TryRequest(this.XmppService.Contracts.GetCreatedContracts);
						ContractReferences = ToReferences(Contracts);
						break;

					case ContractsListMode.SignedContracts:
						(Succeeded, Contracts) = await this.NetworkService.TryRequest(this.XmppService.Contracts.GetSignedContracts);
						ContractReferences = ToReferences(Contracts);
						break;

					case ContractsListMode.ContractTemplates:
						KeyValuePair<DateTime, string>[] Templates;
						(Succeeded, Templates) = await this.NetworkService.TryRequest(this.XmppService.Contracts.GetContractTemplateIds);

						int i = 0;
						int c = Templates.Length;
						ContractReferences = new ContractReference[c];

						foreach (KeyValuePair<DateTime, string> P in Templates)
							ContractReferences[i++] = new ContractReference(P.Key, P.Value);

						break;

					default:
						return;
				}

				if (!Succeeded)
					return;

				if (this.loadContractsTimestamp > now)
					return;

				if (ContractReferences.Length <= 0)
				{
					this.UiSerializer.BeginInvokeOnMainThread(() => this.ShowContractsMissing = true);
					return;
				}

				SortedDictionary<string, List<ContractModel>> ContractsByCategory = new(StringComparer.CurrentCultureIgnoreCase);

				foreach (ContractReference Ref in ContractReferences)
				{
					Contract contract;

					try
					{
						contract = await Ref.GetContract(this.XmppService.Contracts);
					}
					catch (Waher.Networking.XMPP.StanzaErrors.ItemNotFoundException)
					{
						continue;   // Contract not available for some reason. Ignore, and display the rest.
					}

					if (this.loadContractsTimestamp > now)
						return;

					this.contractsMap[Ref.ContractId] = contract;

					ContractModel Item = await ContractModel.Create(Ref.ContractId, Ref.Timestamp, contract, this.TagProfile, this.XmppService);
					string Category = Item.Category;

					if (!ContractsByCategory.TryGetValue(Category, out List<ContractModel> Contracts2))
					{
						Contracts2 = new List<ContractModel>();
						ContractsByCategory[Category] = Contracts2;
					}

					Contracts2.Add(Item);
				}

				List<ContractCategoryModel> Categories = new();

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

		private static ContractReference[] ToReferences(Contract[] Contracts)
		{
			if (Contracts is null)
				return new ContractReference[0];

			ContractReference[] Result = new ContractReference[Contracts.Length];
			int i = 0;

			foreach (Contract Contract in Contracts)
				Result[i++] = new ContractReference(Contract);

			return Result;
		}

	}
}