using System;
using System.Reflection;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Represents an abstraction of a Neuron server. This opens and maintains a connection to a Neuron server.
    /// </summary>
    [DefaultImplementation(typeof(NeuronService))]
    public interface INeuronService : ILoadableService, IDisposable
    {
        /// <summary>
        /// Can be used to <c>await</c> the server's connection state, i.e. skipping all intermediate states but <see cref="XmppState.Connected"/>.
        /// </summary>
        /// <param name="timeout">Maximum timeout before giving up.</param>
        /// <returns></returns>
        Task<bool> WaitForConnectedState(TimeSpan timeout);
        /// <summary>
        /// An event that triggers whenever the connection state of the Neuron server changes.
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        /// <summary>
        /// To be used during the very first phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server.
        /// </summary>
        /// <param name="domain">The server's domain name.</param>
        /// <param name="hostName">The server's host name.</param>
        /// <param name="portNumber">The xmpp port.</param>
        /// <param name="languageCode">Language code to use for communication.</param>
        /// <param name="appAssembly">The current app's main assembly.</param>
        /// <param name="connectedFunc">A callback to use if and when connected.</param>
        /// <returns></returns>
        Task<(bool succeeded, string errorMessage)> TryConnect(string domain, string hostName, int portNumber, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);
        /// <summary>
        /// To be used during the second phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server, while also creating an account.
        /// </summary>
        /// <param name="domain">The server's domain name.</param>
        /// <param name="hostName">The server's host name.</param>
        /// <param name="portNumber">The xmpp port.</param>
        /// <param name="userName">The user name of the account to create.</param>
        /// <param name="password">The password to use.</param>
        /// <param name="languageCode">Language code to use for communication.</param>
        /// <param name="appAssembly">The current app's main assembly.</param>
        /// <param name="connectedFunc">A callback to use if and when connected.</param>
        /// <returns></returns>
        Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, string hostName, int portNumber, string userName, string password, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);
        /// <summary>
        /// To be used during the second phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server, while also connecting to an existing account.
        /// </summary>
        /// <param name="domain">The server's domain name.</param>
        /// <param name="hostName">The server's host name.</param>
        /// <param name="portNumber">The xmpp port.</param>
        /// <param name="userName">The user name of the account to create.</param>
        /// <param name="password">The password to use.</param>
        /// <param name="languageCode">Language code to use for communication.</param>
        /// <param name="appAssembly">The current app's main assembly.</param>
        /// <param name="connectedFunc">A callback to use if and when connected.</param>
        /// <returns></returns>
        Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, string hostName, int portNumber, string userName, string password, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);

        /// <summary>
        /// Logs out the current user, shutting down the XMPP connection.
        /// </summary>
        /// <returns></returns>
        Task LogOut();
        /// <summary>
        /// Logs in the current user, re-establishing te XMPP connection.
        /// </summary>
        /// <returns></returns>
        Task LogIn();

        /// <summary>
        /// Determines whether the user has logged out or not.
        /// </summary>
        bool IsLoggedOut { get; }
        /// <summary>
        /// Determines whether the Neuron server is online or not.
        /// </summary>
        bool IsOnline { get; }
        /// <summary>
        /// The current state of the Neuron server.
        /// </summary>
        XmppState State { get; }
        /// <summary>
        /// The Bare Jid of the current connection, or <c>null</c>.
        /// </summary>
        string BareJId { get; }
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
        INeuronChats Chats { get; }

        /// <summary>
        /// Run this method to discover services for any given Neuron server.
        /// </summary>
        /// <param name="client">The client to use. Can be <c>null</c>, in which case the default is used.</param>
        /// <returns></returns>
        Task<bool> DiscoverServices(XmppClient client = null);
        /// <summary>
        /// Creates a dump of the latest Xmpp communication as html.
        /// </summary>
        /// <returns>The communication dump as a html formatted string.</returns>
        string CommsDumpAsHtml();

        /// <summary>
        /// Perform a shutdown in critical situations. Attempts to shut down XMPP connection as fast as possible.
        /// </summary>
        /// <returns></returns>
        Task UnloadFast();
    }
}