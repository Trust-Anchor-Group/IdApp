using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Provisioning.SearchOperators;

namespace IdApp.Services.ThingRegistries
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
		/// Checks if a URI is a direct reference URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <returns>If <paramref name="DiscoUri"/> is a direct reference URI.</returns>
		bool IsIoTDiscoDirectURI(string DiscoUri);

		/// <summary>
		/// Tries to decode an IoTDisco Claim URI (subset of all possible IoTDisco URIs).
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="Tags">Decoded meta data tags.</param>
		/// <returns>If DiscoUri was successfully decoded.</returns>
		bool TryDecodeIoTDiscoClaimURI(string DiscoUri, out MetaDataTag[] Tags);

		/// <summary>
		/// Tries to decode an IoTDisco Search URI (subset of all possible IoTDisco URIs).
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="Operators">Search operators.</param>
		/// <param name="RegistryJid">Registry Service JID</param>
		/// <returns>If the URI could be parsed.</returns>
		bool TryDecodeIoTDiscoSearchURI(string DiscoUri, out SearchOperator[] Operators, out string RegistryJid);

		/// <summary>
		/// Tries to decode an IoTDisco Direct Reference URI (subset of all possible IoTDisco URIs).
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="Jid">JID of device</param>
		/// <param name="SourceId">Optional Source ID of device, or null if none.</param>
		/// <param name="NodeId">Optional Node ID of device, or null if none.</param>
		/// <param name="PartitionId">Optional Partition ID of device, or null if none.</param>
		/// <returns>If the URI could be parsed.</returns>
		bool TryDecodeIoTDiscoDirectURI(string DiscoUri, out string Jid, out string SourceId, out string NodeId, out string PartitionId);

		/// <summary>
		/// Claims a think in accordance with parameters defined in a iotdisco claim URI.
		/// </summary>
		/// <param name="DiscoUri">IoTDisco URI</param>
		/// <param name="MakePublic">If the device should be public in the thing registry.</param>
		/// <returns>Information about the thing, or error if unable.</returns>
		Task<NodeResultEventArgs> ClaimThing(string DiscoUri, bool MakePublic);

		/// <summary>
		/// Disowns a thing
		/// </summary>
		/// <param name="RegistryJid">Registry JID</param>
		/// <param name="ThingJid">Thing JID</param>
		/// <param name="SourceId">Source ID</param>
		/// <param name="Partition">Partition</param>
		/// <param name="NodeId">Node ID</param>
		/// <returns>If the thing was disowned</returns>
		Task<bool> Disown(string RegistryJid, string ThingJid, string SourceId, string Partition, string NodeId);

		/// <summary>
		/// Searches for devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="Offset">Start offset of list</param>
		/// <param name="MaxCount">Maximum number of items in response.</param>
		/// <param name="DiscoUri">iotdisco URI.</param>
		/// <returns>Devices found, Registry JID, and if more devices are available.</returns>
		Task<(SearchResultThing[], string, bool)> Search(int Offset, int MaxCount, string DiscoUri);

		/// <summary>
		/// Searches for devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="Offset">Start offset of list</param>
		/// <param name="MaxCount">Maximum number of items in response.</param>
		/// <param name="RegistryJid">Registry Service JID</param>
		/// <param name="Operators">Search operators.</param>
		/// <returns>Devices found, and if more devices are available.</returns>
		Task<(SearchResultThing[], bool)> Search(int Offset, int MaxCount, string RegistryJid, params SearchOperator[] Operators);

		/// <summary>
		/// Searches for all devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="DiscoUri">iotdisco URI.</param>
		/// <returns>Complete list of devices in registry matching the search operators, and the JID of the registry service.</returns>
		Task<(SearchResultThing[], string)> SearchAll(string DiscoUri);

		/// <summary>
		/// Searches for all devices in accordance with settings in a iotdisco-URI.
		/// </summary>
		/// <param name="RegistryJid">Registry Service JID</param>
		/// <param name="Operators">Search operators.</param>
		/// <returns>Complete list of devices in registry matching the search operators.</returns>
		Task<SearchResultThing[]> SearchAll(string RegistryJid, params SearchOperator[] Operators);

	}
}