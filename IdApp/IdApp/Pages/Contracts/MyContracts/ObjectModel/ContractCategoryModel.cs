using IdApp.Resx;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.Pages.Contracts.MyContracts.ObjectModel
{
    /// <summary>
    /// The data model for a contract category containing a set of contracts of the same category.
    /// </summary>
    public class ContractCategoryModel : ObservableRangeCollection<ContractModel>, INotifyPropertyChanged
    {
        private bool expanded;

        /// <summary>
        /// Creates an instance of the <see cref="ContractCategoryModel"/> class.
        /// </summary>
        /// <param name="Category">Contract category</param>
        /// <param name="Contracts">Contracts in category.</param>
        public ContractCategoryModel(string Category, ICollection<ContractModel> Contracts)
        {
            this.Category = Category;
            this.Contracts = Contracts;
        }

        /// <summary>
        /// Displayable category for the contracts.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Displayable category for the contracts.
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
                    //this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Expanded)));
                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Symbol)));

                    if (this.expanded)
                    {
                        this.AddRange(this.Contracts);
                    }
                    else
                    {
                        this.Clear();
                    }
                }
            }
		}

        /// <summary>
        /// Symbol used for the category.
        /// </summary>
        public string Symbol => this.expanded ? FontAwesome.FolderOpen : FontAwesome.Folder;
    }
}
