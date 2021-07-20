using IdApp.Pages.Contracts.MyContracts;

namespace IdApp.Pages.Contracts.ContractTemplates
{
    /// <summary>
    /// A page that displays a list of contract templates used by the current user.
    /// </summary>
    public class ContractTemplatesPage : MyContractsPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ContractTemplatesPage"/> class.
        /// </summary>
        public ContractTemplatesPage()
            : base(ContractsListMode.ContractTemplates)
        {
        }
    }
}