using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Services.Contracts;
using IdApp.Services.Navigation;
using IdApp.Services.Notification;
using IdApp.Services.Notification.Contracts;
using NeuroFeatures;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.CommunityToolkit.Helpers;
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
		private Contract selectedContract = null;

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsViewModel"/> class.
		/// </summary>
		protected internal MyContractsViewModel()
		{
			this.IsBusy = true;
			this.Action = SelectContractAction.ViewContract;
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.IsBusy = true;
			this.ShowContractsMissing = false;

			if (this.NavigationService.TryGetArgs(out MyContractsNavigationArgs args))
			{
				this.contractsListMode = args.Mode;
				this.Action = args.Action;
				this.selection = args.Selection;

				switch (this.contractsListMode)
				{
					case ContractsListMode.Contracts:
						this.Title = LocalizationResourceManager.Current["Contracts"];
						this.Description = LocalizationResourceManager.Current["ContractsInfoText"];
						break;

					case ContractsListMode.ContractTemplates:
						this.Title = LocalizationResourceManager.Current["ContractTemplates"];
						this.Description = LocalizationResourceManager.Current["ContractTemplatesInfoText"];
						break;

					case ContractsListMode.TokenCreationTemplates:
						this.Title = LocalizationResourceManager.Current["TokenCreationTemplates"];
						this.Description = LocalizationResourceManager.Current["TokenCreationTemplatesInfoText"];
						break;
				}
			}

			await this.LoadContracts();

			this.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted += this.NotificationService_OnNotificationsDeleted;
		}

		/// <inheritdoc/>
		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			if (this.selection is not null && this.selection.Task.IsCompleted)
			{
				await this.NavigationService.GoBackAsync();
				return;
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted -= this.NotificationService_OnNotificationsDeleted;

			if (this.Action != SelectContractAction.Select)
			{
				this.ShowContractsMissing = false;
				this.contractsMap.Clear();
			}

			this.selection?.TrySetResult(this.selectedContract);

			await base.OnDispose();
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
						this.Categories.Insert(++Index, Contract);
				}
				else
				{
					foreach (ContractModel Contract in Category.Contracts)
						this.Categories.Remove(Contract);
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
							if (this.contractsListMode == ContractsListMode.Contracts)
							{
								//!!!!!!
								ViewContractNavigationArgs Args = new(Contract, false);

								await this.NavigationService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Pop);
							}
							else
							{
								//!!!!!!
								NewContractNavigationArgs Args = new(Contract, null);

								await this.NavigationService.GoToAsync(nameof(NewContractPage), Args, BackMethod.ToThisPage);
							}
							break;

						case SelectContractAction.Select:
							this.selectedContract = Contract;
							await this.NavigationService.GoBackAsync();
							this.selection?.TrySetResult(Contract);
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
				Contract Contract;

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

					case ContractsListMode.TokenCreationTemplates:
						ContractReferences = await Database.Find<ContractReference>(new FilterAnd(
							new FilterFieldEqualTo("IsTemplate", true),
							new FilterFieldEqualTo("ContractLoaded", true)));

						Dictionary<CaseInsensitiveString, bool> ContractIds = new();
						LinkedList<ContractReference> TokenCreationTemplates = new();

						foreach (ContractReference Ref in ContractReferences)
						{
							if (!Ref.IsTokenCreationTemplate.HasValue)
							{
								if (!Ref.IsTemplate)
									Ref.IsTokenCreationTemplate = false;
								else
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

									Ref.IsTokenCreationTemplate =
										Contract.ForMachinesLocalName == "Create" &&
										Contract.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures;
								}

								await Database.Update(Ref);
							}

							if (Ref.IsTokenCreationTemplate.Value)
							{
								ContractIds[Ref.ContractId] = true;
								TokenCreationTemplates.AddLast(Ref);
							}
						}

						foreach (string TokenTemplateId in Constants.ContractTemplates.TokenCreationTemplates)
						{
							if (!ContractIds.ContainsKey(TokenTemplateId))
							{
								Contract = await this.XmppService.GetContract(TokenTemplateId);

								ContractReference Ref = new()
								{
									ContractId = Contract.ContractId
								};

								await Ref.SetContract(Contract, this);
								await Database.Insert(Ref);

								if (Ref.IsTokenCreationTemplate.Value)
								{
									ContractIds[Ref.ContractId] = true;
									TokenCreationTemplates.AddLast(Ref);
								}
							}
						}

						ContractReferences = TokenCreationTemplates;
						ShowAdditionalEvents = false;
						break;

					default:
						return;
				}

				SortedDictionary<string, List<ContractModel>> ContractsByCategory = new(StringComparer.CurrentCultureIgnoreCase);
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCategory = this.NotificationService.GetEventsByCategory(EventButton.Contracts);
				bool Found = false;

				foreach (ContractReference Ref in ContractReferences)
				{
					Found = true;

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
					{
						EventsByCategory.Remove(Ref.ContractId);

						List<NotificationEvent> Events2 = new();
						List<NotificationEvent> Petitions = null;

						foreach (NotificationEvent Event in Events)
						{
							if (Event is ContractPetitionNotificationEvent Petition)
							{
								Petitions ??= new List<NotificationEvent>();
								Petitions.Add(Petition);
							}
							else
								Events2.Add(Event);
						}

						if (Petitions is not null)
							EventsByCategory[Ref.ContractId] = Petitions.ToArray();

						Events = Events2.ToArray();
					}
					else
						Events = new NotificationEvent[0];

					ContractModel Item = await ContractModel.Create(Ref.ContractId, Ref.Created, Contract, this, Events, this);
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
							string Description = await Event.GetDescription(this);

							NewCategories.Add(new EventModel(Event.Received, Icon, Description, Event, this));
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
					this.ShowContractsMissing = !Found;
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

		private void NotificationService_OnNotificationsDeleted(object Sender, NotificationEventsArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				foreach (NotificationEvent Event in e.Events)
				{
					if (Event.Button != EventButton.Contracts)
						continue;

					HeaderModel LastHeader = null;

					foreach (IItemGroup Group in this.Categories)
					{
						if (Group is HeaderModel Header)
							LastHeader = Header;
						else if (Group is ContractModel Contract && Contract.ContractId == Event.Category)
						{
							if (Contract.RemoveEvent(Event) && LastHeader is not null)
								LastHeader.NrEvents--;

							break;
						}
					}
				}
			});
		}

		private void NotificationService_OnNewNotification(object Sender, NotificationEventArgs e)
		{
			if (e.Event.Button != EventButton.Contracts)
				return;

			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				HeaderModel LastHeader = null;

				foreach (IItemGroup Group in this.Categories)
				{
					if (Group is HeaderModel Header)
						LastHeader = Header;
					else if (Group is ContractModel Contract && Contract.ContractId == e.Event.Category)
					{
						if (Contract.AddEvent(e.Event) && LastHeader is not null)
							LastHeader.NrEvents++;

						break;
					}
				}
			});
		}

	}
}
