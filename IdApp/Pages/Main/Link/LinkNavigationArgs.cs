using IdApp.Services.Navigation;

namespace IdApp.Pages.Main.Link
{
	/// <summary>
	/// Holds navigation parameters for the link view.
	/// </summary>
	public class LinkNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Holds navigation parameters for the link view.
		/// </summary>
		public LinkNavigationArgs()
			: this(null, null)
		{
		}

		/// <summary>
		/// Holds navigation parameters for the link view.
		/// </summary>
		/// <param name="Url">Url to display.</param>
		/// <param name="Title">Title</param>
		public LinkNavigationArgs(string Url, string Title)
		{
			this.Url = Url;
			this.Title = Title;
		}

		/// <summary>
		/// URL to display
		/// </summary>
		public string Url { get; }

		/// <summary>
		/// Title to display
		/// </summary>
		public string Title { get; }
	}
}
