using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace IdApp.Pages.Contracts.MyContracts.ObjectModel
{
    /// <summary>
    /// The data model for a contract category containing a set of contracts of the same category.
    /// </summary>
    public class ContractCategoryModel : ObservableCollection<ContractModel>, INotifyPropertyChanged
    {
        private readonly string category;
        private readonly ContractModel[] contracts;
        private bool expanded = false;

        /// <summary>
        /// Creates an instance of the <see cref="ContractCategoryModel"/> class.
        /// </summary>
        /// <param name="Category">Contract category</param>
        /// <param name="Contracts">Contracts in category.</param>
        public ContractCategoryModel(string Category, params ContractModel[] Contracts)
        {
            this.category = Category;
            this.contracts = Contracts;
        }

        /// <summary>
        /// Displayable category for the contracts.
        /// </summary>
        public string Category => this.category;

        /// <summary>
        /// Number of contracts in category.
        /// </summary>
        public int NrContracts => this.contracts.Length;

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
                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Expanded)));
                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Symbol)));

                    if (this.expanded)
                    {
                        foreach (ContractModel Contract in this.contracts)
                            this.Add(Contract);
                    }
                    else
                        this.Clear();
                }
            }
		}

        /// <summary>
        /// Symbol used for the category.
        /// </summary>
        public string Symbol => this.expanded ? FontAwesome.FolderOpen : FontAwesome.Folder;

    }
}