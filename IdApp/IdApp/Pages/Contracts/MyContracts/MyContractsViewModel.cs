using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Resx;
using IdApp.Services.Contracts;
using IdApp.Services.Notification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;

namespace IdApp.Pages.Contracts.MyContracts
{
	/// <summary>
	/// The view model to bind to when displaying 'my' contracts.
	/// </summary>
	public class MyContractsViewModel : BaseViewModel
	{
		private readonly Dictionary<string, Contract> contractsMap = new();
		private ContractsListMode contractsListMode;
		private TaskCompletionSource<Contract> selection;

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsViewModel"/> class.
		/// </summary>
		protected internal MyContractsViewModel()
		{
			this.IsBusy = true;
			this.Action = SelectContractAction.ViewContract;
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			this.IsBusy = true;
			this.ShowContractsMissing = false;

			if (this.NavigationService.TryPopArgs(out MyContractsNavigationArgs args))
			{
				this.contractsListMode = args.Mode;
				this.Action = args.Action;
				this.selection = args.Selection;

				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						this.Title = AppResources.Contracts;
						this.Description = AppResources.ContractsInfoText;
						break;

					case ContractsListMode.ContractTemplates:
						this.Title = AppResources.ContractTemplates;
						this.Description = AppResources.ContractTemplatesInfoText;
						break;
				}
			}

			await this.LoadContracts();
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			if (this.Action != SelectContractAction.Select)
			{
				this.ShowContractsMissing = false;
				this.contractsMap.Clear();
			}

			await base.DoUnbind();
		}

		/// <summary>
		/// Method called when closing view model, returning to a previous view.
		/// </summary>
		public override void OnClosingPage()
		{
			this.selection?.TrySetResult(null);

			base.OnClosingPage();
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
			get => (string)this.GetValue(TitleProperty);
			set => this.SetValue(TitleProperty, value);
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
			get => (string)this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
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
			get => (SelectContractAction)this.GetValue(ActionProperty);
			set => this.SetValue(ActionProperty, value);
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
			get => (bool)this.GetValue(ShowContractsMissingProperty);
			set => this.SetValue(ShowContractsMissingProperty, value);
		}

		/// <summary>
		/// Holds the list of contracts to display, ordered by category.
		/// </summary>
		public ObservableRangeCollection<IItemGroup> Categories { get; } = new();

		/// <summary>
		/// Add or remove the contracts from the collection
		/// </summary>
		public void AddOrRemoveContracts(HeaderModel Category, bool Expanded)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				if (Expanded)
				{
					int Index = this.Categories.IndexOf(Category);

					foreach (ContractModel Contract in Category.Contracts)
					{
						this.Categories.Insert(++Index, Contract);
					}
				}
				else
				{
					foreach (ContractModel Contract in Category.Contracts)
					{
						this.Categories.Remove(Contract);
					}
				}
			});
		}
		/// <summary>
		/// Add or remove the contracts from the collection
		/// </summary>
		public void ContractSelected(string ContractId)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				if (this.contractsMap.TryGetValue(ContractId, out Contract Contract))
				{
					switch (this.Action)
					{
						case SelectContractAction.ViewContract:
							if (this.contractsListMode == ContractsListMode.ContractTemplates)
							{
								await this.NavigationService.GoToAsync(
									nameof(NewContractPage), new NewContractNavigationArgs(Contract, null));
							}
							else
							{
								await this.NavigationService.GoToAsync(
									nameof(ViewContractPage), new ViewContractNavigationArgs(Contract, false));
							}
							break;

						case SelectContractAction.Select:
							this.selection?.TrySetResult(Contract);
							await this.NavigationService.GoBackAsync();
							break;
					}
				}
			});
		}

		private async Task LoadContracts()
		{
			try
			{
				IEnumerable<ContractReference> ContractReferences;
				bool ShowAdditionalEvents;

				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						ContractReferences = await Database.Find<ContractReference>(new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", false),
							new FilterFieldEqualTo("ContractLoaded", true)));

						ShowAdditionalEvents = true;
						break;

					case ContractsListMode.ContractTemplates:
						ContractReferences = await Database.Find<ContractReference>(new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true)));

						ShowAdditionalEvents = false;
						break;

					default:
						return;
				}

				bool Found = false;

				foreach (ContractReference Ref in ContractReferences)
				{
					Found = true;
					break;
				}

				if (!Found)
				{
					this.UiSerializer.BeginInvokeOnMainThread(() => this.ShowContractsMissing = true);
					return;
				}

				SortedDictionary<string, List<ContractModel>> ContractsByCategory = new(StringComparer.CurrentCultureIgnoreCase);
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCategory = this.NotificationService.GetEventsByCategory(EventButton.Contracts);
				Contract Contract;

				foreach (ContractReference Ref in ContractReferences)
				{
					try
					{
						Contract = await Ref.GetContract();
						if (Contract is null)
							continue;
					}
					catch (Exception ex)
					{
						this.LogService.LogException(ex);
						continue;
					}

					this.contractsMap[Ref.ContractId] = Contract;

					if (EventsByCategory.TryGetValue(Ref.ContractId, out NotificationEvent[] Events))
						EventsByCategory.Remove(Ref.ContractId);
					else
						Events = new NotificationEvent[0];

					ContractModel Item = await ContractModel.Create(Ref.ContractId, Ref.Created, Contract, this, Events);
					string Category = Item.Category;

					if (!ContractsByCategory.TryGetValue(Category, out List<ContractModel> Contracts2))
					{
						Contracts2 = new List<ContractModel>();
						ContractsByCategory[Category] = Contracts2;
					}

					Contracts2.Add(Item);
				}

				List<IItemGroup> NewCategories = new();

				if (ShowAdditionalEvents)
				{
					foreach (KeyValuePair<CaseInsensitiveString, NotificationEvent[]> P in EventsByCategory)
					{
						foreach (NotificationEvent Event in P.Value)
						{
							string Icon = await Event.GetCategoryIcon(this);
							string Description = await Event.GetCategoryDescription(this);

							NewCategories.Add(new EventModel(Event.Received, Icon, Description, Event));
						}
					}
				}

				foreach (KeyValuePair<string, List<ContractModel>> P in ContractsByCategory)
				{
					int Nr = 0;

					foreach (ContractModel Model in P.Value)
						Nr += Model.NrEvents;

					P.Value.Sort(new DateTimeDesc());
					NewCategories.Add(new HeaderModel(P.Key, P.Value.ToArray(), Nr));
				}

				this.UiSerializer.BeginInvokeOnMainThread(() =>
				{
					this.Categories.Clear();
					this.Categories.AddRange(NewCategories);
				});
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

	}
}
