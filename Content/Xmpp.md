# XMPP #
[XMPP](https://xmpp.org/about/technology-overview.html) is the Extensible Messaging and Presence Protocol,
a set of open technologies for instant messaging, presence, multi-party chat, voice and video calls, collaboration, 
lightweight middleware, content syndication, and generalized routing of XML data.

The keywords here are _xml_, _decentralized_ and _extensible_.

All communication is done via xml. This means a connection to an XMPP server
must be maintained and kept alive. In TAGs case, this server is called a `Neuron` server.

Accessing the `Neuron` server is done via the [`ITagIdSdk`](../Tag.Neuron.Xamarin/ITagIdSdk.cs)s [`INeuronService`](../Tag.Neuron.Xamarin/Services/INeuronService.cs). The `Neuron` server provides
xml communication, and via the _extensibility_ features of the XMPP Protocol other features can be discovered (the `INeuronService.DiscoverServices()` method is used for this).

Some of the extensions that have been added to the `Neuron` server is handling of
Digital IDs and Digital Contracts.

A user's Digital ID is managed via the [ITagProfile](../Tag.Neuron.Xamarin/Services/ITagProfile.cs). Digital Contracts
are handled via the [INeuronContracts](../Tag.Neuron.Xamarin/Services/INeuronContracts.cs) part of the
[INeuronService](../Tag.Neuron.Xamarin/Services/INeuronService.cs). It exposes various methods and events
for viewing, approving, revoking digital contracts and IDs.

## Next Steps ##
To explore more about TAGs XMPP implementation, have a look at this documentation:
