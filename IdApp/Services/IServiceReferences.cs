using IdApp.Services.AttachmentCache;
using IdApp.Services.Contracts;
using IdApp.Services.Crypto;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Xmpp;
using IdApp.Services.Nfc;
using IdApp.Services.Settings;
using IdApp.Services.Storage;
using IdApp.Services.Tag;
using IdApp.Services.ThingRegistries;
using IdApp.Services.UI;
using IdApp.Services.Wallet;
using IdApp.Services.Push;
using IdApp.Services.Notification;

namespace IdApp.Services
{
    /// <summary>
    /// Interface for classes that reference services in the app.
    /// </summary>
    public interface IServiceReferences
    {
        /// <summary>
        /// The dispatcher to use for alerts and accessing the main thread.
        /// </summary>
        IUiSerializer UiSerializer { get; }

        /// <summary>
        /// The XMPP service for XMPP communication.
        /// </summary>
        public IXmppService XmppService { get; }

		/// <summary>
		/// TAG Profie service.
		/// </summary>
		public ITagProfile TagProfile { get; }

		/// <summary>
		/// Navigation service.
		/// </summary>
		public INavigationService NavigationService { get; }

		/// <summary>
		/// Log service.
		/// </summary>
		public ILogService LogService { get; }

		/// <summary>
		/// Network service.
		/// </summary>
		public INetworkService NetworkService { get; }

		/// <summary>
		/// Neuro-Wallet orchestrator service.
		/// </summary>
		public INeuroWalletOrchestratorService NeuroWalletOrchestratorService { get; }

		/// <summary>
		/// Contract orchestrator service.
		/// </summary>
		public IContractOrchestratorService ContractOrchestratorService { get; }

		/// <summary>
		/// Thing Registry orchestrator service.
		/// </summary>
		public IThingRegistryOrchestratorService ThingRegistryOrchestratorService { get; }

		/// <summary>
		/// AttachmentCache service.
		/// </summary>
		public IAttachmentCacheService AttachmentCacheService { get; }

		/// <summary>
		/// Crypto service.
		/// </summary>
		public ICryptoService CryptoService { get; }

		/// <summary>
		/// Settings service.
		/// </summary>
		public ISettingsService SettingsService { get; }

		/// <summary>
		/// Storage service.
		/// </summary>
		public IStorageService StorageService { get; }

		/// <summary>
		/// Near-Field Communication (NFC) service.
		/// </summary>
		public INfcService NfcService { get; }

		/// <summary>
		/// Notification Service
		/// </summary>
		public INotificationService NotificationService { get; }

		/// <summary>
		/// Push Notification Service
		/// </summary>
		public IPushNotificationService PushNotificationService { get; }

	}
}
