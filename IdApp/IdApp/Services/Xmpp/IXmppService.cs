﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using IdApp.Services.Contracts;
using IdApp.Services.IoT;
using IdApp.Services.Push;
using IdApp.Services.ThingRegistries;
using IdApp.Services.Wallet;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.PEP;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Xmpp
{
    /// <summary>
    /// Represents an abstraction of a connection to an XMPP Server.
    /// </summary>
    [DefaultImplementation(typeof(XmppService))]
    public interface IXmppService : ILoadableService, IServiceReferences
    {
		#region Lifecycle

		/// <summary>
		/// Can be used to <c>await</c> the server's connection state, i.e. skipping all intermediate states but <see cref="XmppState.Connected"/>.
		/// </summary>
		/// <param name="timeout">Maximum timeout before giving up.</param>
		/// <returns>If connected</returns>
		Task<bool> WaitForConnectedState(TimeSpan timeout);

		/// <summary>
		/// Perform a shutdown in critical situations. Attempts to shut down XMPP connection as fast as possible.
		/// </summary>
		Task UnloadFast();

		#endregion

		#region Events

		/// <summary>
		/// An event that triggers whenever the connection state to the XMPP server changes.
		/// </summary>
		event StateChangedEventHandler ConnectionStateChanged;

		/// <summary>
		/// Event raised when a new presence stanza has been received.
		/// </summary>
		event PresenceEventHandlerAsync OnPresence;

		/// <summary>
		/// Event raised when a roster item has been added to the roster.
		/// </summary>
		event RosterItemEventHandlerAsync OnRosterItemAdded;

		/// <summary>
		/// Event raised when a roster item has been updated in the roster.
		/// </summary>
		event RosterItemEventHandlerAsync OnRosterItemUpdated;

		/// <summary>
		/// Event raised when a roster item has been removed from the roster.
		/// </summary>
		event RosterItemEventHandlerAsync OnRosterItemRemoved;

		#endregion

		#region State

		/// <summary>
		/// Determines whether the connection to the XMPP server is live or not.
		/// </summary>
		bool IsOnline { get; }

		/// <summary>
		/// The current state of the connection to the XMPP server.
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
		ISmartContracts Contracts { get; }

		/// <summary>
		/// Provides access to thing registries.
		/// </summary>
		IXmppThingRegistry ThingRegistry { get; }

		/// <summary>
		/// Provides access to provisioning and decision support.
		/// </summary>
		IIoTService IoT { get; }

		/// <summary>
		/// Provides access to the eDaler wallet.
		/// </summary>
		INeuroWallet Wallet { get; }

		#endregion

		#region Connections

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

		#endregion

		#region Components & Services

		/// <summary>
		/// Run this method to discover services for any given XMPP server.
		/// </summary>
		/// <param name="client">The client to use. Can be <c>null</c>, in which case the default is used.</param>
		/// <returns>If TAG services were found.</returns>
		Task<bool> DiscoverServices(XmppClient client = null);

		#endregion

		#region Transfer

		/// <summary>
		/// Registers a Transfer ID Code
		/// </summary>
		/// <param name="Code">Transfer Code</param>
		Task AddTransferCode(string Code);

		#endregion

		#region Push Notification

		/// <summary>
		/// Registers a new token with the back-end broker.
		/// </summary>
		/// <param name="TokenInformation">Token Information</param>
		/// <returns>If token could be registered.</returns>
		Task<bool> NewPushNotificationToken(TokenInformation TokenInformation);

		#endregion

		#region Tokens

		/// <summary>
		/// Gets a token for use with APIs that are either distributed or use different
		/// protocols, when the client needs to authenticate itself using the current
		/// XMPP connection.
		/// </summary>
		/// <param name="Seconds">Number of seconds for which the token should be valid.</param>
		/// <returns>Token, if able to get a token, or null otherwise.</returns>
		Task<string> GetApiToken(int Seconds);

		/// <summary>
		/// Performs an HTTP POST to a protected API on the server, over the current XMPP connection,
		/// authenticating the client using the credentials alreaedy provided over XMPP.
		/// </summary>
		/// <param name="LocalResource">Local Resource on the server to POST to.</param>
		/// <param name="Data">Data to post. This will be encoded using encoders in the type inventory.</param>
		/// <param name="Headers">Headers to provide in the POST.</param>
		/// <returns>Decoded response from the resource.</returns>
		/// <exception cref="Exception">Any communication error will be handle by raising the corresponding exception.</exception>
		Task<object> PostToProtectedApi(string LocalResource, object Data, params KeyValuePair<string, string>[] Headers);

		#endregion

		#region HTTP File Upload

		/// <summary>
		/// Returns <c>true</c> if file upload is supported, <c>false</c> otherwise.
		/// </summary>
		bool FileUploadIsSupported { get; }

		/// <summary>
		/// Uploads a file to the upload component.
		/// </summary>
		/// <param name="FileName">Name of file.</param>
		/// <param name="ContentType">Internet content type.</param>
		/// <param name="ContentSize">Size of content.</param>
		Task<HttpFileUploadEventArgs> RequestUploadSlotAsync(string FileName, string ContentType, long ContentSize);

		#endregion

		#region Personal Eventing Protocol (PEP)

		/// <summary>
		/// Registers an event handler of a specific type of personal events.
		/// </summary>
		/// <param name="PersonalEventType">Type of personal event.</param>
		/// <param name="Handler">Event handler.</param>
		void RegisterPepHandler(Type PersonalEventType, PersonalEventNotificationEventHandler Handler);

		/// <summary>
		/// Unregisters an event handler of a specific type of personal events.
		/// </summary>
		/// <param name="PersonalEventType">Type of personal event.</param>
		/// <param name="Handler">Event handler.</param>
		/// <returns>If the event handler was found and removed.</returns>
		bool UnregisterPepHandler(Type PersonalEventType, PersonalEventNotificationEventHandler Handler);

		#endregion
	}
}
