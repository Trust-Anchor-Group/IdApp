using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation
{
    /// <summary>
    /// Holds navigation parameters specific to claiming things.
    /// </summary>
    public class ViewClaimThingNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ViewClaimThingNavigationArgs"/> class.
        /// </summary>
        /// <param name="Uri">iotdisco URI contining the claim parameters.</param>
        public ViewClaimThingNavigationArgs(string Uri)
        {
            this.Uri = Uri;
        }
        
        /// <summary>
        /// The iotdisco claim URI
        /// </summary>
        public string Uri { get; }
    }
}