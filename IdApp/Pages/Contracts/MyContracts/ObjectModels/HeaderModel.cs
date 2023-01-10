using IdApp.Resx;
using System.Collections.Generic;
using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// The data model for a contract category header containing a collection of contracts of the same category.
	/// </summary>
	public class HeaderModel : ObservableObject, IItemGroup
	{
		private int nrEvents;
		private bool expanded;

		/// <summary>
		/// Creates an instance of the <see cref="HeaderModel"/> class.
		/// </summary>
		/// <param name="Category">Contract category</param>
		/// <param name="Contracts">Contracts in category.</param>
		/// <param name="NrEvents">Number of events</param>
		public HeaderModel(string Category, ICollection<ContractModel> Contracts, int NrEvents)
		{
			this.Category = Category;
			this.Contracts = Contracts;
			this.nrEvents = NrEvents;
		}

		/// <summary>
		/// Displayable category name for the contracts.
		/// </summary>
		public string Category { get; }

		/// <inheritdoc/>
		public string UniqueName => this.Category;

		/// <summary>
		/// Number of events.
		/// </summary>
		public int NrEvents
		{
			get => this.nrEvents;
			set
			{
				if (this.nrEvents != value)
				{
					bool b1 = this.nrEvents > 0;
					bool b2 = value > 0;

					this.nrEvents = value;
					this.OnPropertyChanged(nameof(this.NrEvents));

					if (b1 ^ b2)
						this.OnPropertyChanged(nameof(this.HasEvents));
				}
			}
		}

		/// <summary>
		/// If the category contains contracts with events.
		/// </summary>
		public bool HasEvents => this.nrEvents > 0;

		/// <summary>
		/// The category's contracts.
		/// </summary>
		public ICollection<ContractModel> Contracts { get; }

		/// <summary>
		/// Number of contracts in category.
		/// </summary>
		public int NrContracts => this.Contracts.Count;

		/// <summary>
		/// If the group is expanded or not.
		/// </summary>
		public bool Expanded
		{
			get => this.expanded;
			set
			{
				if (this.expanded != value)
				{
					this.expanded = value;
					this.OnPropertyChanged(nameof(this.Symbol));
				}
			}
		}

		/// <summary>
		/// Symbol used for the category.
		/// </summary>
		public string Symbol => this.expanded ? FontAwesome.FolderOpen : FontAwesome.Folder;
	}
}
