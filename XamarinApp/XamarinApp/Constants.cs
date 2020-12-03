using System;

namespace XamarinApp
{
    public static class Constants
    {
        public static class LanguageCodes
        {
            public const string Default = "en-US";
        }

        public static class IoTSchemes
        {
            public const string IotId = "iotid";
            public const string IotDisco = "iotdisco";
            public const string IotSc = "iotsc";
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
            public static readonly TimeSpan XmppPresence = TimeSpan.FromSeconds(1);
            public static readonly TimeSpan UploadFile = TimeSpan.FromSeconds(30);
            public static readonly TimeSpan DownloadFile = TimeSpan.FromSeconds(10);
        }
    }
}