using IdApp.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Contracts.ViewContract
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a contract.
	/// </summary>
	public class ViewContractNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="ViewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="contract">The contract to display.</param>
		/// <param name="isReadOnly"><c>true</c> if the contract is readonly, <c>false</c> otherwise.</param>
		public ViewContractNavigationArgs(Contract contract, bool isReadOnly)
			: this(contract, isReadOnly, false, string.Empty)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ViewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="contract">The contract to display.</param>
		/// <param name="isReadOnly"><c>true</c> if the contract is readonly, <c>false</c> otherwise.</param>
		/// <param name="isProposal">If contract is a proposal.</param>
		/// <param name="Proposal">Proposal text.</param>
		public ViewContractNavigationArgs(Contract contract, bool isReadOnly, bool isProposal, string Proposal)
		{
			this.Contract = contract;
			this.IsReadOnly = isReadOnly;
			this.IsProposal = isProposal;
			this.Proposal = Proposal;
		}

		/// <summary>
		/// The contract to display.
		/// </summary>
		public Contract Contract { get; }

		/// <summary>
		/// <c>true</c> if the contract is readonly, <c>false</c> otherwise.
		/// </summary>
		public bool IsReadOnly { get; }

		/// <summary>
		/// <c>true</c> if the contract is a proposal, <c>false</c> otherwise.
		/// </summary>
		public bool IsProposal { get; }

		/// <summary>
		/// Proposal text.
		/// </summary>
		public string Proposal { get; }
	}
}