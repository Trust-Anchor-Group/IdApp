using System.Collections.Generic;
using Waher.Persistence;

namespace IdApp.Pages.Contracts
{
	/// <summary>
	/// Interface for pages that can receive contract options from an asynchronous process.
	/// </summary>
	public interface IContractOptionsPage
	{
		/// <summary>
		/// Method called (from main thread) when contract options are made available.
		/// </summary>
		/// <param name="Options">Available options, as dictionaries with contract parameters.</param>
		void ShowContractOptions(IDictionary<CaseInsensitiveString, object>[] Options);
	}
}
