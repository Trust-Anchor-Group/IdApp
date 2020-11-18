﻿using System;

namespace XamarinApp
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