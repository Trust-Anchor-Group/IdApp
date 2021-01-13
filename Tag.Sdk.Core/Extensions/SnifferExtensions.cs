using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Waher.Networking.Sniffers;
using Waher.Networking.Sniffers.Model;

namespace Tag.Sdk.Core.Extensions
{
    public static class SnifferExtensions
    {
        public static string SnifferToText(this InMemorySniffer sniffer)
        {
            if (sniffer == null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            using (StringWriter writer = new StringWriter(sb))
            using (TextWriterSniffer output = new TextWriterSniffer(writer, BinaryPresentationMethod.ByteCount))
            {
                sniffer.Replay(output);
            }

            return sb.ToString();
        }

        public static string SnifferLatestToText(this InMemorySniffer sniffer)
        {
            if (sniffer == null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            SnifferEvent lastEvent = null;
            using (var e = sniffer.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (e.Current is SnifferRxText)
                        lastEvent = e.Current;
                }
            }

            SnifferRxText rxText = lastEvent as SnifferRxText;
            if (rxText != null)
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ConformanceLevel = ConformanceLevel.Fragment;

                using (StringReader stringReader = new StringReader(rxText.Text))
                using (XmlReader reader = XmlReader.Create(stringReader, settings))
                {
                    XDocument doc = XDocument.Load(reader);
                    if (doc.Root != null)
                        sb.AppendLine(doc.Root.Value);
                }
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
            if (sniffer == null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            using (XmlWriterSniffer output = new XmlWriterSniffer(writer, BinaryPresentationMethod.ByteCount))
            {
                sniffer.Replay(output);
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