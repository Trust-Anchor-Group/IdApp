namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A page that displays a list of contracts signed by the current user.
    /// </summary>
    public class SignedContractsPage : MyContractsPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SignedContractsPage"/> class.
        /// </summary>
        public SignedContractsPage()
            : base(ViewModels.Contracts.ContractsListMode.SignedContracts)
        {
        }
    }
}