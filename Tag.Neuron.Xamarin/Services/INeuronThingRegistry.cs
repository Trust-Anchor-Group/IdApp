using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Provisioning.SearchOperators;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Adds support for XMPP Thing Registries
    /// </summary>
    [DefaultImplementation(typeof(NeuronThingRegistry))]
    public interface INeuronThingRegistry
    {
		/// <summary>
		/// Checks if a URI is a claim URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <returns>If <paramref name="DiscoUri"/> is a claim URI.</returns>
		bool IsIoTDiscoClaimURI(string DiscoUri);

		/// <summary>
		/// Checks if a URI is a search URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <returns>If <paramref name="DiscoUri"/> is a search URI.</returns>
		bool IsIoTDiscoSearchURI(string DiscoUri);

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

		/// <summary>
		/// Claims a think in accordance with parameters defined in a iotdisco claim URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="MakePublic">If the device should be public in the thing registry.</param>
		/// <returns>If OK, null is returned. If the thing could not be claimed, an error message from the neuron
		/// describing why is returned. Other types of errors, raise exceptions.</returns>
		Task<string> ClaimThing(string DiscoUri, bool MakePublic);
	}
}