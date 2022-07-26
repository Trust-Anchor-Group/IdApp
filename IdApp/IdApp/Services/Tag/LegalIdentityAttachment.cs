using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;

namespace IdApp.Services.Tag
{
	/// <summary>
	/// Represent an attachment to a <see cref="LegalIdentity"/>.
	/// </summary>
	[CollectionName("LegalAttachments")]
	public sealed class LegalIdentityAttachment
	{
		/// <summary>
		/// Creates an instance of a <see cref="LegalIdentityAttachment"/>.
		/// </summary>
		public LegalIdentityAttachment() { }

		/// <summary>
		/// Creates an instance of a <see cref="LegalIdentityAttachment"/>.
		/// </summary>
		/// <param name="fileName">The actual filename</param>
		/// <param name="contentType">The content type.</param>
		/// <param name="data">The raw file data.</param>
		public LegalIdentityAttachment(string fileName, string contentType, byte[] data)
		{
			this.Filename = fileName;
			this.ContentType = contentType;
			this.Data = data;
			this.ContentLength = data?.Length ?? 0;
		}

		/// <summary>
		/// The primary key in persistent storage.
		/// </summary>
		[ObjectId]
		public string ObjectId { get; set; }

		/// <summary>
		/// The raw filename.
		/// </summary>
		public string Filename { get; set; }

		/// <summary>
		/// Content type (mime) of the attachment.
		/// </summary>
		public string ContentType { get; set; }

		/// <summary>
		/// The raw file data.
		/// </summary>
		public byte[] Data { get; set; }

		/// <summary>
		/// Attachment content length.
		/// </summary>
		public long ContentLength { get; set; }
	}
}
