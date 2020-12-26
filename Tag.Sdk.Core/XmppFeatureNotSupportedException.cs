using System;

namespace Tag.Sdk.Core
{
    public class XmppFeatureNotSupportedException : Exception
    {
        public XmppFeatureNotSupportedException()
        : base()
        {
        }

        public XmppFeatureNotSupportedException(string message)
        : base(message)
        {
        }

        public XmppFeatureNotSupportedException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}