using System;
using System.Collections.Generic;
using Waher.Runtime.Inventory;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Provisioning.SearchOperators;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Adds support for Xmpp Chat functionality.
    /// </summary>
    [DefaultImplementation(typeof(NeuronThingRegistry))]
    public interface INeuronThingRegistry
    {
		/// <summary>
		/// Tries to decode an IoTDisco Claim URI (subset of all possible IoTDisco URIs).
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="Tags">Decoded meta data tags.</param>
		/// <returns>If DiscoUri was successfully decoded.</returns>
		bool TryDecodeIoTDiscoClaimURI(string DiscoUri, out MetaDataTag[] Tags);

		/// <summary>
		/// Decodes an IoTDisco URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="Operators">Search operators.</param>
		/// <returns>If the URI could be parsed.</returns>
		bool TryDecodeIoTDiscoSearchURI(string DiscoUri, out IEnumerable<SearchOperator> Operators);
	}
}