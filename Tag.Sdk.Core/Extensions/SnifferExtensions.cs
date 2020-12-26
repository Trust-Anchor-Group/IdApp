using System.IO;
using System.Text;
using System.Xml;
using Waher.Networking.Sniffers;

namespace Tag.Sdk.Core.Extensions
{
    public static class SnifferExtensions
    {
		public static string SnifferToText(this InMemorySniffer sniffer)
        {
            StringBuilder sb = new StringBuilder();

            using (StringWriter writer = new StringWriter(sb))
            using (TextWriterSniffer output = new TextWriterSniffer(writer, BinaryPresentationMethod.ByteCount))
            {
                sniffer?.Replay(output);
            }

            return sb.ToString();
        }

        public static string SnifferToXml(this InMemorySniffer sniffer)
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = false
            };

            return SnifferToXml(sniffer, settings);
        }

        internal static string SnifferToXml(InMemorySniffer sniffer, XmlWriterSettings settings)
        {
            StringBuilder sb = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            using (XmlWriterSniffer output = new XmlWriterSniffer(writer, BinaryPresentationMethod.ByteCount))
            {
                sniffer?.Replay(output);
            }

            return sb.ToString();
        }

        internal static void SaveSnifferAsText(InMemorySniffer sniffer, string fileName)
        {
            using (TextFileSniffer output = new TextFileSniffer(fileName, BinaryPresentationMethod.ByteCount))
            {
                sniffer?.Replay(output);
            }
        }

        internal static void SaveSnifferAsXml(InMemorySniffer sniffer, string fileName)
        {
            using (XmlFileSniffer output = new XmlFileSniffer(fileName, BinaryPresentationMethod.ByteCount))
            {
                sniffer?.Replay(output);
            }
        }

        internal static void SaveSnifferAsXml(InMemorySniffer sniffer, string fileName, string xslTransform)
        {
            using (XmlFileSniffer output = new XmlFileSniffer(fileName, xslTransform, BinaryPresentationMethod.ByteCount))
            {
                sniffer?.Replay(output);
            }
        }
	}
}