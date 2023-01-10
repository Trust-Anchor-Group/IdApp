using IdApp.Services.Navigation;
using System.Collections.Generic;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace IdApp.Pages.Contracts.NewContract
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a new contract.
	/// </summary>
	public class NewContractNavigationArgs : NavigationArgs
	{
		private List<CaseInsensitiveString> suppressProposals = null;

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		public NewContractNavigationArgs()
			: this(null, false, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		public NewContractNavigationArgs(Dictionary<CaseInsensitiveString, object> ParameterValues)
			: this(null, false, ParameterValues)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="Template">The contract to use as template.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		public NewContractNavigationArgs(Contract Template, Dictionary<CaseInsensitiveString, object> ParameterValues)
			: this(Template, false, ParameterValues)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="Template">The contract to use as template.</param>
		/// <param name="SetVisibility">If visibility should be set by default.</param>
		/// <param name="ParameterValues">Parameter values to set in new contract.</param>
		public NewContractNavigationArgs(Contract Template, bool SetVisibility, Dictionary<CaseInsensitiveString, object> ParameterValues)
		{
			this.Template = Template;
			this.SetVisibility = SetVisibility;
			this.ParameterValues = ParameterValues;
		}

		/// <summary>
		/// The contract to use as template.
		/// </summary>
		public Contract Template { get; }

		/// <summary>
		/// If visibility should be set by default.
		/// </summary>
		public bool SetVisibility { get; }

		/// <summary>
		/// Parameter values to set in new contract.
		/// </summary>
		public Dictionary<CaseInsensitiveString, object> ParameterValues { get; }

		/// <summary>
		/// Any legal IDs to whom proposals should not be sent. May be null if no proposals should be suppressed.
		/// </summary>
		public CaseInsensitiveString[] SuppressedProposalLegalIds
		{
			get => this.suppressProposals?.ToArray();
		}

		/// <summary>
		/// Suppresses a proposal for a given Legal ID.
		/// </summary>
		/// <param name="LegalId">Legal ID to whom a proposal should not be sent.</param>
		public void SuppressProposal(CaseInsensitiveString LegalId)
		{
			if (this.suppressProposals is null)
				this.suppressProposals = new List<CaseInsensitiveString>();

			this.suppressProposals.Add(LegalId);
		}
	}
}
