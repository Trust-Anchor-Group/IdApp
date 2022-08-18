using System.Threading.Tasks;

namespace IdApp.Pages
{
	/// <summary>
	/// Interface for modal views.
	/// </summary>
	public interface IModalView
	{
		/// <summary>
		/// Method called when closing view model, returning to a previous view.
		/// </summary>
		void OnClosingPage();
	}
}
