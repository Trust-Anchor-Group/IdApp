using IdApp.Pages.Contracts.MyContracts;

namespace IdApp.Pages.Contracts.SignedContracts
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
            : base(MyContracts.ContractsListMode.SignedContracts)
        {
        }
    }
}