using EDaler.Uris;
using IdApp.Services.Navigation;

namespace IdApp.Pages.Wallet
{
	/// <summary>
	/// Holds navigation parameters specific to eDaler URIs.
	/// </summary>
	public class EDalerUriNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="EDalerUriNavigationArgs"/> class.
		/// </summary>
		/// <param name="Uri">Parsed edaler URI.</param>
		public EDalerUriNavigationArgs(EDalerUri Uri)
			: this(Uri, string.Empty)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="EDalerUriNavigationArgs"/> class.
		/// </summary>
		/// <param name="Uri">Parsed edaler URI.</param>
		/// <param name="FriendlyName">Optional Friendly Name associated with URI</param>
		public EDalerUriNavigationArgs(EDalerUri Uri, string FriendlyName)
		{
			this.Uri = Uri;
			this.FriendlyName = FriendlyName;
		}

		/// <summary>
		/// The edaler URI
		/// </summary>
		public EDalerUri Uri { get; }

		/// <summary>
		/// Optional Friendly Name associated with URI
		/// </summary>
		public string FriendlyName { get; }
	}
}