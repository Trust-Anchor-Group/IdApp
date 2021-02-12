using System;
using System.IO;
using System.Text;
using System.Xml;
using Waher.Networking.Sniffers;

namespace Tag.Neuron.Xamarin.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="ISniffer"/> implementation.
    /// </summary>
    public static class SnifferExtensions
    {
        /// <summary>
        /// Converts the latest Xmpp communication that the sniffer holds to plain text.
        /// </summary>
        /// <param name="sniffer">The sniffer whose contents to get.</param>
        /// <returns>The xmpp communication in plain text.</returns>
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

        /// <summary>
        /// Converts the latest Xmpp communication that the sniffer holds to xml.
        /// </summary>
        /// <param name="sniffer">The sniffer whose contents to get.</param>
        /// <returns>The xmpp communication in xml.</returns>
        public static string SnifferToXml(this InMemorySniffer sniffer)
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "  ",
                ConformanceLevel = ConformanceLevel.Auto,
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
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