using System;
using System.Reflection;
using System.Threading.Tasks;
using IdApp.Services.Contracts;
using IdApp.Services.Provisioning;
using IdApp.Services.ThingRegistries;
using IdApp.Services.Wallet;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Neuron
{
    /// <summary>
    /// Represents an abstraction of a Neuron server. This opens and maintains a connection to a Neuron server.
    /// </summary>
    [DefaultImplementation(typeof(NeuronService))]
    public interface INeuronService : ILoadableService
    {
        /// <summary>
        /// Can be used to <c>await</c> the server's connection state, i.e. skipping all intermediate states but <see cref="XmppState.Connected"/>.
        /// </summary>
        /// <param name="timeout">Maximum timeout before giving up.</param>
        /// <returns>If connected</returns>
        Task<bool> WaitForConnectedState(TimeSpan timeout);
        
        /// <summary>
        /// An event that triggers whenever the connection state of the Neuron server changes.
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        
        /// <summary>
        /// To be used during the very first phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server.
        /// </summary>
        /// <param name="domain">The server's domain name.</param>
        /// <param name="isIpAddress">If the domain is provided as an IP address.</param>
        /// <param name="hostName">The server's host name.</param>
        /// <param name="portNumber">The xmpp port.</param>
        /// <param name="languageCode">Language code to use for communication.</param>
        /// <param name="appAssembly">The current app's main assembly.</param>
        /// <param name="connectedFunc">A callback to use if and when connected.</param>
        /// <returns>If connected. If not, any error message.</returns>
        Task<(bool succeeded, string errorMessage)> TryConnect(string domain, bool isIpAddress, string hostName, int portNumber, 
            string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);

        /// <summary>
        /// To be used during the second phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server, while also creating an account.
        /// </summary>
        /// <param name="domain">The server's domain name.</param>
        /// <param name="isIpAddress">If the domain is provided as an IP address.</param>
        /// <param name="hostName">The server's host name.</param>
        /// <param name="portNumber">The xmpp port.</param>
        /// <param name="userName">The user name of the account to create.</param>
        /// <param name="password">The password to use.</param>
        /// <param name="languageCode">Language code to use for communication.</param>
        /// <param name="ApiKey">API Key used when creating account.</param>
        /// <param name="ApiSecret">API Secret used when creating account.</param>
        /// <param name="appAssembly">The current app's main assembly.</param>
        /// <param name="connectedFunc">A callback to use if and when connected.</param>
        /// <returns>If connected. If not, any error message.</returns>
        Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, bool isIpAddress, string hostName, 
            int portNumber, string userName, string password, string languageCode, string ApiKey, string ApiSecret, 
            Assembly appAssembly, Func<XmppClient, Task> connectedFunc);

        /// <summary>
        /// To be used during the second phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server, while also connecting to an existing account.
        /// </summary>
        /// <param name="domain">The server's domain name.</param>
        /// <param name="isIpAddress">If the domain is provided as an IP address.</param>
        /// <param name="hostName">The server's host name.</param>
        /// <param name="portNumber">The xmpp port.</param>
        /// <param name="userName">The user name of the account to create.</param>
        /// <param name="password">The password to use.</param>
        /// <param name="passwordMethod">The password hash method to use. Empty string signifies an unhashed password.</param>
        /// <param name="languageCode">Language code to use for communication.</param>
        /// <param name="appAssembly">The current app's main assembly.</param>
        /// <param name="connectedFunc">A callback to use if and when connected.</param>
        /// <returns>If connected. If not, any error message.</returns>
        Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, bool isIpAddress, string hostName, 
            int portNumber, string userName, string password, string passwordMethod, string languageCode, Assembly appAssembly, 
            Func<XmppClient, Task> connectedFunc);

        /// <summary>
        /// Determines whether the Neuron server is online or not.
        /// </summary>
        bool IsOnline { get; }
        
        /// <summary>
        /// The current state of the Neuron server.
        /// </summary>
        XmppState State { get; }

        /// <summary>
        /// Interface to the XMPP network.
        /// </summary>
        XmppClient Xmpp { get; }
        
        /// <summary>
        /// The Bare Jid of the current connection, or <c>null</c>.
        /// </summary>
        string BareJid { get; }
        
        /// <summary>
        /// The latest generic xmpp error, if any.
        /// </summary>
        string LatestError { get; }
        
        /// <summary>
        /// The latest generic xmpp connection error, if any.
        /// </summary>
        string LatestConnectionError { get; }
        
        /// <summary>
        /// Provides access to legal identities and contracts.
        /// </summary>
        INeuronContracts Contracts { get; }

        /// <summary>
        /// Provides access to chat functionality.
        /// </summary>
        INeuronMultiUserChat MultiUserChat { get; }

        /// <summary>
        /// Provides access to thing registries.
        /// </summary>
        INeuronThingRegistry ThingRegistry { get; }

        /// <summary>
        /// Provides access to provisioning and decision support.
        /// </summary>
        INeuronProvisioningService Provisioning { get; }

        /// <summary>
        /// Provides access to the eDaler wallet.
        /// </summary>
        INeuronWallet Wallet { get; }

        /// <summary>
        /// Run this method to discover services for any given Neuron server.
        /// </summary>
        /// <param name="client">The client to use. Can be <c>null</c>, in which case the default is used.</param>
        /// <returns>If TAG services were found.</returns>
        Task<bool> DiscoverServices(XmppClient client = null);

        /// <summary>
        /// Creates a dump of the latest Xmpp communication as html.
        /// </summary>
        /// <returns>The communication dump as a html formatted string.</returns>
        Task<string> CommsDumpAsHtml(bool history = false);

        /// <summary>
        /// saves already sent html to history.
        /// </summary>        
        void ClearHtmlContent();

        /// <summary>
        /// Creates a dump of the latest Xmpp communication as Text.
        /// </summary>
        /// <returns>The communication dump as a text formatted string.</returns>
        Task<string> CommsDumpAsText(string state);

        /// <summary>
        /// Perform a shutdown in critical situations. Attempts to shut down XMPP connection as fast as possible.
        /// </summary>
        Task UnloadFast();

        /// <summary>
        /// Registers a Transfer ID Code
        /// </summary>
        /// <param name="Code">Transfer Code</param>
        Task AddTransferCode(string Code);
    }
}