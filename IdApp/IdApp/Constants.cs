﻿using System;

namespace IdApp
{
    /// <summary>
    /// A set of never changing property constants and helpful values.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// A generic "no value available" string.
        /// </summary>
        public const string NotAvailableValue = "-";

		/// <summary>
		/// A maximum number of pixels to render for images, downscaling them if necessary.
		/// </summary>
		public const int MaxRenderedImageDimensionInPixels = 800;

        /// <summary>
        /// Authentication constants
        /// </summary>
        public static class Authentication
        {
            /// <summary>
            /// Minimum length for PIN Code
            /// </summary>
            public const int MinPinLength = 8;

			/// <summary>
			/// Minimum number of symbols from at least two character classes (digits, letters, other) in a PIN.
			/// </summary>
			public const int MinPinSymbolsFromDifferentClasses = 2;

			/// <summary>
			/// Maximum number of identical symbols in a PIN.
			/// </summary>
			public const int MaxPinIdenticalSymbols = 2;

			/// <summary>
			/// Maximum number of sequenced symbols in a PIN.
			/// </summary>
			public const int MaxPinSequencedSymbols = 2;
		}

        /// <summary>
        /// Language Codes
        /// </summary>
        public static class LanguageCodes
        {
            /// <summary>
            /// The default language code.
            /// </summary>
            public const string Default = "en-US";
        }

        /// <summary>
        /// IoT Schemes
        /// </summary>
        public static class UriSchemes
        {
			/// <summary>
			/// The App's URI Scheme (tagidapp)
			/// </summary>
			public const string UriSchemeTagIdApp = "tagidapp";

			/// <summary>
			/// The IoT ID URI Scheme (iotid)
			/// </summary>
			public const string UriSchemeIotId = "iotid";

            /// <summary>
            /// The IoT Discovery URI Scheme (iotdisco)
            /// </summary>
            public const string UriSchemeIotDisco = "iotdisco";

            /// <summary>
            /// The IoT Smart Contract URI Scheme (iotsc)
            /// </summary>
            public const string UriSchemeIotSc = "iotsc";

            /// <summary>
            /// TAG Signature (Quick-Login) URI Scheme (tagsign)
            /// </summary>
            public const string UriSchemeTagSign = "tagsign";

            /// <summary>
            /// eDaler URI Scheme (edaler)
            /// </summary>
            public const string UriSchemeEDaler = "edaler";

            /// <summary>
            /// eDaler URI Scheme (edaler)
            /// </summary>
            public const string UriSchemeNeuroFeature = "nfeat";

            /// <summary>
            /// Onboarding URI Scheme (obinfo)
            /// </summary>
            public const string UriSchemeOnboarding = "obinfo";

            /// <summary>
            /// XMPP URI Scheme (xmpp)
            /// </summary>
            public const string UriSchemeXmpp = "xmpp";

			/// <summary>
			/// AES-256-encrypted data.
			/// </summary>
			public const string Aes256 = "aes256";

            /// <summary>
            /// Gets the predefined scheme from an IoT Code
            /// </summary>
            /// <param name="Url">The URL to parse.</param>
            /// <returns>URI Scheme</returns>
            public static string GetScheme(string Url)
            {
                if (string.IsNullOrWhiteSpace(Url))
                    return null;

                int i = Url.IndexOf(':');
                if (i < 0)
                    return null;

                Url = Url[..i].ToLowerInvariant();

				return Url switch
				{
					UriSchemeIotId or
                    UriSchemeIotDisco or
                    UriSchemeIotSc or
                    UriSchemeTagSign or
                    UriSchemeEDaler or
                    UriSchemeNeuroFeature or
                    UriSchemeOnboarding or
                    UriSchemeXmpp or
					UriSchemeTagIdApp => Url,

					_ => null,
				};
			}

            /// <summary>
            /// Checks if the specified code starts with the IoT ID scheme.
            /// </summary>
            /// <param name="Url">The URL to check.</param>
            /// <returns>If URI is an ID scheme</returns>
            public static bool StartsWithIdScheme(string Url)
            {
                return !string.IsNullOrWhiteSpace(Url) &&
                       Url.StartsWith(UriSchemeIotId + ":", StringComparison.InvariantCultureIgnoreCase);
            }

            /// <summary>
            /// Generates a IoT Scan Uri form the specified id.
            /// </summary>
            /// <param name="id">The Id to use when generating the Uri.</param>
            /// <returns>Smart Contract URI</returns>
            public static string CreateSmartContractUri(string id)
            {
                return UriSchemeIotSc + ":" + id;
            }

            /// <summary>
            /// Generates a IoT ID Uri form the specified id.
            /// </summary>
            /// <param name="id">The Id to use when generating the Uri.</param>
            /// <returns>Identity URI</returns>
            public static string CreateIdUri(string id)
            {
                return UriSchemeIotId + ":" + id;
            }

            /// <summary>
            /// Generates a Neuro-Feature ID Uri form the specified id.
            /// </summary>
            /// <param name="id">The Id to use when generating the Uri.</param>
            /// <returns>Neuro-Feature URI</returns>
            public static string CreateTokenUri(string id)
            {
                return UriSchemeNeuroFeature + ":" + id;
            }

            /// <summary>
            /// Removes the URI Schema from an URL.
            /// </summary>
            /// <param name="Url">The URL to parse and extract the URI schema from.</param>
            /// <returns>URI, without schema</returns>
            public static string RemoveScheme(string Url)
            {
                string Scheme = GetScheme(Url);
                if (string.IsNullOrEmpty(Scheme))
                    return null;

                return Url[(Scheme.Length + 1)..];
            }
        }

        /// <summary>
        /// MIME Types
        /// </summary>
        public static class MimeTypes
        {
            /// <summary>
            /// The JPEG MIME type.
            /// </summary>
            public const string Jpeg = "image/jpeg";

            /// <summary>
            /// The PNG MIME type.
            /// </summary>
            public const string Png = "image/png";
        }

        /// <summary>
        /// Domain names.
        /// </summary>
        public static class Domains
		{
            /// <summary>
            /// TAG ID domain.
            /// </summary>
            public const string IdDomain = "id.tagroot.io";

            /// <summary>
            /// TAG ID onboarding domain.
            /// </summary>
            public const string OnboardingDomain = "onboarding.id.tagroot.io";
        }

        /// <summary>
        /// XMPP Protocol Properties.
        /// </summary>
        public static class XmppProperties
        {
            /// <summary>
            /// First name
            /// </summary>
            public const string FirstName = "FIRST";

            /// <summary>
            /// Middle name
            /// </summary>
            public const string MiddleName = "MIDDLE";

            /// <summary>
            /// Last name
            /// </summary>
            public const string LastName = "LAST";

            /// <summary>
            /// /Personal number
            /// </summary>
            public const string PersonalNumber = "PNR";

            /// <summary>
            /// Address line 1
            /// </summary>
            public const string Address = "ADDR";

            /// <summary>
            /// Address line 2
            /// </summary>
            public const string Address2 = "ADDR2";

            /// <summary>
            /// Area
            /// </summary>
            public const string Area = "AREA";

            /// <summary>
            /// City
            /// </summary>
            public const string City = "CITY";

            /// <summary>
            /// Zip Code
            /// </summary>
            public const string ZipCode = "ZIP";

            /// <summary>
            ///  Region
            /// </summary>
            public const string Region = "REGION";

            /// <summary>
            /// Country
            /// </summary>
            public const string Country = "COUNTRY";

            /// <summary>
            /// Device ID
            /// </summary>
            public const string DeviceId = "DEVICE_ID";

            /// <summary>
            /// Jabber ID
            /// </summary>
            public const string Jid = "JID";

            /// <summary>
            /// Phone number
            /// </summary>
            public const string Phone = "PHONE";

			/// <summary>
			/// e-Mail address
			/// </summary>
			public const string EMail = "EMAIL";
		}

		/// <summary>
		/// Timer Intervals
		/// </summary>
		public static class Intervals
        {
            /// <summary>
            /// Auto Save interval
            /// </summary>
            public static readonly TimeSpan AutoSave = TimeSpan.FromSeconds(1);

            /// <summary>
            /// Reconnect interval
            /// </summary>
            public static readonly TimeSpan Reconnect = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Timer Timeout Values
        /// </summary>
        public static class Timeouts
        {
            /// <summary>
            /// Generic request timeout
            /// </summary>
            public static readonly TimeSpan GenericRequest = TimeSpan.FromSeconds(30);

            /// <summary>
            /// Database timeout
            /// </summary>
            public static readonly TimeSpan Database = TimeSpan.FromSeconds(10);

            /// <summary>
            /// XMPP Connect timeout
            /// </summary>
            public static readonly TimeSpan XmppConnect = TimeSpan.FromSeconds(10);

            /// <summary>
            /// XMPP Init timeout
            /// </summary>
            public static readonly TimeSpan XmppInit = TimeSpan.FromSeconds(1);

            /// <summary>
            /// Upload file timeout
            /// </summary>
            public static readonly TimeSpan UploadFile = TimeSpan.FromSeconds(30);

            /// <summary>
            /// Download file timeout
            /// </summary>
            public static readonly TimeSpan DownloadFile = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// MessagingCenter events
        /// </summary>
        public static class MessagingCenter
        {
			/// <summary>
			/// Request to focus the chat editor control
			/// </summary>
			public const string ChatEditorFocus = "ChatEditorFocus";

			/// <summary>
			/// Request to unfocus the chat editor control
			/// </summary>
			public const string ChatEditorUnfocus = "ChatEditorUnfocus";

			/// <summary>
			/// Keyboard appears event
			/// </summary>
			public const string KeyboardAppears = "KeyboardAppears";

            /// <summary>
            /// Keyboard disappears event
            /// </summary>
            public const string KeyboardDisappears = "KeyboardDisappears";
        }

        /// <summary>
        /// Size constants.
        /// </summary>
        public static class BatchSizes
		{
            /// <summary>
            /// Number of messages to load in a single batch.
            /// </summary>
            public const int MessageBatchSize = 30;

            /// <summary>
            /// Number of tokens to load in a single batch.
            /// </summary>
            public const int TokenBatchSize = 10;

            /// <summary>
            /// Number of account events to load in a single batch.
            /// </summary>
            public const int AccountEventBatchSize = 10;

            /// <summary>
            /// Number of devices to load in a single batch.
            /// </summary>
            public const int DeviceBatchSize = 100;
		}

		/// <summary>
		/// Machine-readable names in contracts.
		/// </summary>
		public static class ContractMachineNames
		{
			/// <summary>
			/// Namespace for payment instructions
			/// </summary>
			public const string PaymentInstructionsNamespace = "https://paiwise.tagroot.io/Schema/PaymentInstructions.xsd";

			/// <summary>
			/// Local name for contracts for buying eDaler.
			/// </summary>
			public const string BuyEDaler = "BuyEDaler";

			/// <summary>
			/// Local name for contracts for selling eDaler.
			/// </summary>
			public const string SellEDaler = "SellEDaler";
		}

		/// <summary>
		/// Contract templates
		/// </summary>
		public static class ContractTemplates
        {
            /// <summary>
            /// Contract template for creating a demo token
            /// </summary>
            public const string CreateDemoTokenTemplate = "2bb9fff1-8716-cb1b-5807-9fdb05b2207b@legal.lab.tagroot.io";

			/// <summary>
			/// Contract template for creating five demo tokens
			/// </summary>
			public const string CreateDemoTokens5Template = "2bba00ac-8716-cb3e-5807-9fdb055370c4@legal.lab.tagroot.io";

			/// <summary>
			/// Array of contract templates for creating tokens.
			/// </summary>
			public readonly static string[] TokenCreationTemplates = new string[]
			{
				CreateDemoTokenTemplate,
				CreateDemoTokens5Template
			};

			/// <summary>
			/// Contract template for transferring a token from a seller to a buyer
			/// </summary>
			public const string TransferTokenTemplate = "2a6d6b09-cae9-bb7e-4015-a272cd9cd5b9@legal.lab.tagroot.io";

			/// <summary>
			/// Contract template for consigning the token to an auctioneer with the purpose of selling it.
			/// </summary>
			public const string TokenConsignmentTemplate = "2a6d86d3-cae9-be05-4015-a272cd0cbbb9@legal.lab.tagroot.io";
		}

		/// <summary>
		/// Push chennels
		/// </summary>
		public static class PushChannels
		{
			/// <summary>
			/// Messages channel
			/// </summary>
			public const string Messages = "Messages";

			/// <summary>
			/// Petitions channel
			/// </summary>
			public const string Petitions = "Petitions";

			/// <summary>
			/// Identities channel
			/// </summary>
			public const string Identities = "Identities";

			/// <summary>
			/// Contracts channel
			/// </summary>
			public const string Contracts = "Contracts";

			/// <summary>
			/// eDaler channel
			/// </summary>
			public const string EDaler = "eDaler";

			/// <summary>
			/// Tokens channel
			/// </summary>
			public const string Tokens = "Tokens";

			/// <summary>
			/// Provisioning channel
			/// </summary>
			public const string Provisioning = "Provisioning";
		}

		/// <summary>
		/// Names of Effects.
		/// </summary>
		public static class Effects
		{
			/// <summary>
			/// ResolutionGroupName used for resolving Effects.
			/// </summary>
			public const string ResolutionGroupName = "com.tag.IdApp";

			/// <summary>
			/// PasswordMaskTogglerEffect.
			/// </summary>
			public const string PasswordMaskTogglerEffect = "PasswordMaskTogglerEffect";
		}

		/// <summary>
		/// Constants for PIN
		/// </summary>
		public static class Pin
		{

			/// <summary>
			/// Possible time of inactivity
			/// </summary>
			public const int PossibleInactivityInMinutes = 5;

            /// <summary>
			/// Maximum pin enetring attempts
			/// </summary>
			public const int MaxPinAttempts = 3;

            /// <summary>
			/// First Block in days after 3 attempts
			/// </summary>
			public const int FirstBlockInDays = 1;

            /// <summary>
            /// Second Block in days after 3 attempts
            /// </summary>
            public const int SecondBlockInDays = 7;

            /// <summary>
            /// Key for pin attempt counter
            /// </summary>
            public const string CurrentPinAttemptCounter = "CurrentPinAttemptCounter";

            /// <summary>
            /// Log Object ID
            /// </summary>
            public const string LogAuditorObjectID = "LogAuditorObjectID";

            /// <summary>
            /// Endpoint for LogAuditor
            /// </summary>
            public const string RemoteEndpoint = "local";

            /// <summary>
            /// Protocol for LogAuditor
            /// </summary>
            public const string Protocol = "local";

            /// <summary>
            /// Reason for LogAuditor
            /// </summary>
            public const string Reason = "pinEnteringFailure";
        }

		/// <summary>
		/// References to external resources
		/// </summary>
		public static class References
		{
			/// <summary>
			/// Resource where Android App can be downloaded.
			/// </summary>
			public const string AndroidApp = "https://play.google.com/store/apps/details?id=com.tag.IdApp";

			/// <summary>
			/// Resource where iPhone App can be downloaded.
			/// </summary>
			public const string IPhoneApp = "https://apps.apple.com/se/app/trust-anchor-id/id1580610247";
		}

	}
}
