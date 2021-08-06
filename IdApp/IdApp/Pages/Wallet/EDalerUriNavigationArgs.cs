using IdApp.Services;
using EDaler.Uris;

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
        {
            this.Uri = Uri;
        }
        
        /// <summary>
        /// The edaler URI
        /// </summary>
        public EDalerUri Uri { get; }
    }
}