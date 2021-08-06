using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services
{
    /// <summary>
    /// Represent an attachment to a <see cref="LegalIdentity"/>.
    /// </summary>
    public sealed class LegalIdentityAttachment
    {
        /// <summary>
        /// Creates an instance of a <see cref="LegalIdentityAttachment"/>.
        /// </summary>
        /// <param name="fileName">The actual filename</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="data">The raw file data.</param>
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