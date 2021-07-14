using System;
using System.Collections.ObjectModel;

namespace IdApp.ViewModels.Contracts.ObjectModel
{
    /// <summary>
    /// The data model for a contract category containing a set of contracts of the same category.
    /// </summary>
    public class ContractCategoryModel : ObservableCollection<ContractModel>
    {
        private readonly string category;

        /// <summary>
        /// Creates an instance of the <see cref="ContractCategoryModel"/> class.
        /// </summary>
        /// <param name="Category">Contract category</param>
        /// <param name="Contracts">Contracts in category.</param>
        public ContractCategoryModel(string Category, params ContractModel[] Contracts)
        {
            this.category = Category;

            foreach (ContractModel Contract in Contracts)
                this.Add(Contract);
        }

        /// <summary>
        /// Displayable category for the contracts.
        /// </summary>
        public string Category => this.category;
    }
}