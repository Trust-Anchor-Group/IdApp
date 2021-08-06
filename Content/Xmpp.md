# XMPP #

[XMPP](https://xmpp.org/about/technology-overview.html) is the Extensible Messaging and Presence Protocol,
a set of open technologies for instant messaging, presence, multi-party chat, voice and video calls, collaboration, 
lightweight middleware, content syndication, and generalized routing of XML data.

The keywords here are _xml_, _decentralized_ and _extensible_.

All XMPP communication is done via a single full-duplex connection from the client to a server, called, in the simplest form,
a _broker_ (as what it does is relay infomration between parties in the network). The TAG broker, is also called a `Neuron`, as
it also provides additional services, such as decision support for things, and tools for cross-domain interoperation, such as
digital identities and smart contracts.

As clients always connect to the broker, they can safely reside behind firewalls. Information, in the form of
xml fragments, flow freely over the connection in both directions. Brokers are _federated_, which means that clients 
connected to different brokers can still communicate with each other. The brokers cooperate and exchanges messages 
across domains, much like a very fast version of electronic mail. _Authorization_ based on _consent_ is built into XMPP, 
which means effective communication between parties can only be performed, if consent has been provided earlier. As 
communication can occur spontaneously, in both directions, the connection to the broker must be maintained and kept alive
in order to be able to receive events.

The root elements of the XML fragments being sent and received over the connection (level 2 elements in the overall XML stream, the
level 1 element being the opening tag that initiates an XML stream) are called _stanzas_. There are three different stanzas that
form the basis of XMPP communication: The `message` stanza is used to send asynchronous messages from one point to another. The
`iq` stanza (Information Query) is a request/response mechanism, where one party sends a request to another, which responds to the
first with a result, or an error. The `presence` stanza forms the basis of a simple publish/subscribe mechanism. A sender sends a
`presence` stanza, and the broker forwards it to anyone with an active and authorized presence subscription. This authorization
process forms the basis of the authorization by consent mechanism available by design and by default in XMPP.

Accessing a `Neuron` is done via the [`INeuronService`](../IdApp/Services/INeuronService.cs) instance and methods. Apart from allowing clients to exchange
stanzas, the `Neuron` provides a series of extensions, features and services, in the form of _components_.
These can be discovered and browsed dynamically by the client (the `INeuronService.DiscoverServices()` method is used for this).
Some of the extensions that are made available by the `Neuron` include components that manage _Digital IDs_ and _Smart Contracts_.

A user's Digital ID is managed via the [ITagProfile](../IdApp/Services/ITagProfile.cs) interface. Smart Contracts
are handled via the [INeuronContracts](../IdApp/Services/INeuronContracts.cs) interface, part of the
[INeuronService](../IdApp/Services/INeuronService.cs) interface. It exposes various methods and events
for viewing, approving, revoking digital contracts and IDs.

## Next Steps ##

To explore more about TAGs XMPP implementation, have a look at this documentation:
- [Neuron SDK](NeuronSDK.md)
- [Neuron SDK UI](NeuronSDKUI.md)
- [Creating a TAG Profile](CreatingATAGProfile.md)
