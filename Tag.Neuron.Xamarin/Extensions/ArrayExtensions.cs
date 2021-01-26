using System;
using System.Collections.Generic;
using System.Linq;
using Waher.Networking.XMPP.Contracts;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class ArrayExtensions
    {
        public static IEnumerable<Attachment> GetImageAttachments(this Attachment[] attachments)
        {
            return attachments.Where(x => x.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
        }
    }
}