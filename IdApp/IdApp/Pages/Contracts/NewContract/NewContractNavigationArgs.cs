using IdApp.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Contracts.NewContract
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a new contract.
	/// </summary>
	public class NewContractNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="Template">The contract to use as template.</param>
		public NewContractNavigationArgs(Contract Template)
			: this(Template, false)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="NewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="Template">The contract to use as template.</param>
		/// <param name="SetVisibility">If visibility should be set by default.</param>
		public NewContractNavigationArgs(Contract Template, bool SetVisibility)
		{
			this.Template = Template;
			this.SetVisibility = SetVisibility;
		}

		/// <summary>
		/// The contract to use as template.
		/// </summary>
		public Contract Template { get; }

		/// <summary>
		/// If visibility should be set by default.
		/// </summary>
		public bool SetVisibility { get; }
	}
}