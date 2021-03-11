namespace IdApp.Views.Contracts
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
            : base(ViewModels.Contracts.ContractsListMode.ContractTemplates)
        {
        }
    }
}