using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Networking.Sniffers;

namespace IdApp.Extensions
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
        public static async Task<string> SnifferToText(this InMemorySniffer sniffer)
        {
            if (sniffer is null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            using (StringWriter writer = new StringWriter(sb))
            using (TextWriterSniffer output = new TextWriterSniffer(writer, BinaryPresentationMethod.ByteCount))
            {
                await sniffer.ReplayAsync(output);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the latest Xmpp communication that the sniffer holds to xml.
        /// </summary>
        /// <param name="sniffer">The sniffer whose contents to get.</param>
        /// <returns>The xmpp communication in xml.</returns>
        public static Task<string> SnifferToXml(this InMemorySniffer sniffer)
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

        internal static async Task<string> SnifferToXml(InMemorySniffer sniffer, XmlWriterSettings settings)
        {
            if (sniffer is null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            using (XmlWriterSniffer output = new XmlWriterSniffer(writer, BinaryPresentationMethod.ByteCount))
            {
                await sniffer.ReplayAsync(output);
            }

            return sb.ToString();
        }

        internal static async Task SaveSnifferAsText(InMemorySniffer sniffer, string fileName)
        {
            if (sniffer is null)
                return;

            using (TextFileSniffer output = new TextFileSniffer(fileName, BinaryPresentationMethod.ByteCount))
            {
                await sniffer.ReplayAsync(output);
            }
        }

        internal static async Task SaveSnifferAsXml(InMemorySniffer sniffer, string fileName)
        {
            if (sniffer is null)
                return;

            using (XmlFileSniffer output = new XmlFileSniffer(fileName, BinaryPresentationMethod.ByteCount))
            {
                await sniffer.ReplayAsync(output);
            }
        }

        internal static async Task SaveSnifferAsXml(InMemorySniffer sniffer, string fileName, string xslTransform)
        {
            if (sniffer is null)
                return;

            using (XmlFileSniffer output = new XmlFileSniffer(fileName, xslTransform, BinaryPresentationMethod.ByteCount))
            {
                await sniffer.ReplayAsync(output);
            }
        }
    }
}