using System;
using System.Collections.Generic;
using System.Linq;
using Waher.Networking.XMPP.Contracts;

namespace Tag.Neuron.Xamarin.Extensions
{
    /// <summary>
    /// Extensions for generic <see cref="Array"/>s.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Returns all the attachments whose content type starts with "image".
        /// </summary>
        /// <param name="attachments">The attachments to iterate.</param>
        /// <returns></returns>
        public static IEnumerable<Attachment> GetImageAttachments(this Attachment[] attachments)
        {
            return attachments.Where(x => x.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
        }
    }
}