using System;

namespace Tag.Neuron.Xamarin
{
    public static class Constants
    {
        public const string NotAvailableValue = "-";

        public static class Authentication
        {
            public const int MinPinLength = 8;
        }

        public static class LanguageCodes
        {
            public const string Default = "en-US";
        }

        public static class IoTSchemes
        {
            public const string IotId = "iotid";
            public const string IotDisco = "iotdisco";
            public const string IotSc = "iotsc";

            public static string GetScheme(string code)
            {
                if (!string.IsNullOrWhiteSpace(code))
                {
                    int i = code.IndexOf(':');
                    if (i > 0 && 
                        (code.StartsWith(IotId, StringComparison.InvariantCultureIgnoreCase) ||
                        code.StartsWith(IotDisco, StringComparison.InvariantCultureIgnoreCase) ||
                        code.StartsWith(IotSc, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return code.Substring(0, i).ToLowerInvariant();
                    }
                }

                return null;
            }

            public static bool StartsWithIdScheme(string code)
            {
                return !string.IsNullOrWhiteSpace(code) &&
                       code.StartsWith(IotId + ":", StringComparison.InvariantCultureIgnoreCase);
            }

            public static string CreateScanUri(string id)
            {
                return $"{IotSc}:{id}";
            }

            public static string CreateIdUri(string id)
            {
                return $"{IotId}:{id}";
            }

            public static string GetCode(string code)
            {
                if (!string.IsNullOrWhiteSpace(code))
                {
                    int i = code.IndexOf(':');
                    if (i > 0 && 
                        (code.StartsWith(IotId, StringComparison.InvariantCultureIgnoreCase) ||
                        code.StartsWith(IotDisco, StringComparison.InvariantCultureIgnoreCase) ||
                        code.StartsWith(IotSc, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return code.Substring(i + 1).ToLowerInvariant();
                    }
                }

                return null;
            }
        }

        public static class MimeTypes
        {
            public const string Jpeg = "image/jpeg";
        }

        public static class XmppProperties
        {
            public const string FirstName = "FIRST";
            public const string MiddleName = "MIDDLE";
            public const string LastName = "LAST";
            public const string PersonalNumber = "PNR";
            public const string Address = "ADDR";
            public const string Address2 = "ADDR2";
            public const string Area = "AREA";
            public const string City = "CITY";
            public const string ZipCode = "ZIP";
            public const string Region = "REGION";
            public const string Country = "COUNTRY";
            public const string DeviceId = "DEVICE_ID";
            public const string JId = "JID";
        }

        public static class Intervals
        {
            public static readonly TimeSpan AutoSave = TimeSpan.FromSeconds(1);
            public static readonly TimeSpan Reconnect = TimeSpan.FromSeconds(10);
        }

        public static class Timeouts
        {
            public static readonly TimeSpan Database = TimeSpan.FromSeconds(10);
            public static readonly TimeSpan XmppConnect = TimeSpan.FromSeconds(10);
            public static readonly TimeSpan XmppInit = TimeSpan.FromSeconds(1);
            public static readonly TimeSpan UploadFile = TimeSpan.FromSeconds(30);
            public static readonly TimeSpan DownloadFile = TimeSpan.FromSeconds(10);
        }
    }
}