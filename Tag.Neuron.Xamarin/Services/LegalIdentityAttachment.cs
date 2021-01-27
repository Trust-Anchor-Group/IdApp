using Waher.Networking.XMPP.Contracts;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Represent an attachment to a <see cref="LegalIdentity"/>.
    /// </summary>
    public sealed class LegalIdentityAttachment
    {
        public LegalIdentityAttachment(string fileName, string contentType, byte[] data)
        {
            Filename = fileName;
            ContentType = contentType;
            Data = data;
            ContentLength = data?.Length ?? 0;
        }
        /// <summary>
        /// The raw filename.
        /// </summary>
        public string Filename { get; }
        /// <summary>
        /// Content type (mime) of the attachment.
        /// </summary>
        public string ContentType { get; }
        /// <summary>
        /// The raw file data.
        /// </summary>
        public byte[] Data { get; }
        /// <summary>
        /// Attachment content length.
        /// </summary>
        public long ContentLength { get; }
    }
}