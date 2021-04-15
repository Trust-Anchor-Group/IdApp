using System.Linq;
using Tag.Neuron.Xamarin.Models;

namespace Waher.IoTGateway.Setup
{
    /// <summary>
    /// Represents a user's Xmpp connection and profile properties.
    /// </summary>
    public partial class XmppConfiguration
    {
        /// <summary>
        /// Converts the list of properties on this instance to an array of <see cref="DomainModel"/>s.
        /// </summary>
        /// <returns></returns>
        public static DomainModel[] ToArray()
        {
            return clp.Select(x => new DomainModel(x.Key, x.Value.Key, x.Value.Value)).ToArray();
        }
    }
}