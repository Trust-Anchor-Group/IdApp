using System;

namespace Tag.Neuron.Xamarin
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
        /// Authentication constants
        /// </summary>
        public static class Authentication
        {
            /// <summary>
            /// Minimum length for PIN Code
            /// </summary>
            public const int MinPinLength = 8;
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
        /// Key prefix constants
        /// </summary>
        public static class KeyPrefixes
		{
            /// <summary>
            /// Contract.Template.
            /// </summary>
            public const string ContractTemplatePrefix = "Contract.Template.";
        }

        /// <summary>
        /// IoT Schemes
        /// </summary>
        public static class UriSchemes
        {
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
            /// Gets the predefined scheme from an IoT Code
            /// </summary>
            /// <param name="code">The code to parse.</param>
            /// <returns></returns>
            public static string GetScheme(string code)
            {
                if (string.IsNullOrWhiteSpace(code))
                    return null;

                int i = code.IndexOf(':');
                if (i < 0)
                    return null;

                code = code.Substring(0, i).ToLowerInvariant();

                switch (code)
                {
                    case UriSchemeIotId:
                    case UriSchemeIotDisco:
                    case UriSchemeIotSc:
                    case UriSchemeTagSign:
                    case UriSchemeEDaler:
                        return code;

                    default:
                        return null;
                }
            }

            /// <summary>
            /// Checks if the specified code starts with the IoT ID scheme.
            /// </summary>
            /// <param name="code">The code to parse.</param>
            /// <returns></returns>
            public static bool StartsWithIdScheme(string code)
            {
                return !string.IsNullOrWhiteSpace(code) &&
                       code.StartsWith(UriSchemeIotId + ":", StringComparison.InvariantCultureIgnoreCase);
            }

            /// <summary>
            /// Generates a IoT Scan Uri form the specified id.
            /// </summary>
            /// <param name="id">The Id to use when generating the Uri.</param>
            /// <returns></returns>
            public static string CreateSmartContractUri(string id)
            {
                return $"{UriSchemeIotSc}:{id}";
            }

            /// <summary>
            /// Generates a IoT ID Uri form the specified id.
            /// </summary>
            /// <param name="id">The Id to use when generating the Uri.</param>
            /// <returns></returns>
            public static string CreateIdUri(string id)
            {
                return $"{UriSchemeIotId}:{id}";
            }

            /// <summary>
            /// Returns the raw code, minus the scheme.
            /// </summary>
            /// <param name="code">The code to parse and extract id from.</param>
            /// <returns></returns>
            public static string GetCode(string code)
            {
                string Scheme = GetScheme(code);
                if (string.IsNullOrEmpty(Scheme))
                    return null;

                return code.Substring(Scheme.Length + 1);
            }
        }

        /// <summary>
        /// MIME Types
        /// </summary>
        public static class MimeTypes
        {
            /// <summary>
            /// The Jpeg mime type.
            /// </summary>
            public const string Jpeg = "image/jpeg";
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
            public const string JId = "JID";
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
    }
}