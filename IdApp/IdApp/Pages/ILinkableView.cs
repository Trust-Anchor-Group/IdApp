using System.Threading.Tasks;

namespace IdApp.Pages
{
	/// <summary>
	/// Interfaces for linkable views.
	/// </summary>
	public interface ILinkableView
	{
		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		bool IsLinkable { get; }

		/// <summary>
		/// Link to the current view
		/// </summary>
		string Link { get; }

		/// <summary>
		/// Title of the current view
		/// </summary>
		Task<string> Title { get; }
	}
}
