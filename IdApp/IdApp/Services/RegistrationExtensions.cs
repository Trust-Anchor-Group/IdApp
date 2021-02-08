using System.Linq;
using Tag.Neuron.Xamarin.Models;

namespace Waher.IoTGateway.Setup
{
    public partial class XmppConfiguration
    {
        public DomainModel[] ToArray()
        {
            return clp.Select(x => new DomainModel(x.Key, x.Value.Key, x.Value.Value)).ToArray();
        }
    }
}