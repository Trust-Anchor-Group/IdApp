using System;

namespace IdApp.Exceptions
{
    /// <summary>
    /// Represents a 'feature not supported' exception when communicating with an XMPP Server.
    /// </summary>
    public class XmppFeatureNotSupportedException : Exception
    {
        /// <inheritdoc/>
        public XmppFeatureNotSupportedException()
        : base()
        {
        }

        /// <inheritdoc/>
        public XmppFeatureNotSupportedException(string message)
        : base(message)
        {
        }

        /// <inheritdoc/>
        public XmppFeatureNotSupportedException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}