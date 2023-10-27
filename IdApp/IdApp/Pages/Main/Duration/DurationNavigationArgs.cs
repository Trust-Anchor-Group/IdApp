using IdApp.Services.Navigation;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Duration
{
	/// <summary>
	/// Holds navigation parameters for the duration.
	/// </summary>
	public class DurationNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Holds navigation parameters for the duration.
		/// </summary>
		public DurationNavigationArgs()
			: this(null)
		{
		}

		/// <summary>
		/// Holds navigation parameters for the duration.
		/// </summary>
		/// <param name="Entry">Entry whose value is being calculated.</param>
		public DurationNavigationArgs(Entry Entry)
		{
			this.Entry = Entry;
		}

		/// <summary>
		/// Entry whose value is being calculated.
		/// </summary>
		public Entry Entry { get; }
	}
}
