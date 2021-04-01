using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Provisioning.SearchOperators;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Adds support for XMPP Provisioning
    /// </summary>
    [DefaultImplementation(typeof(NeuronProvisioningService))]
    public interface INeuronProvisioningService
	{
	}
}