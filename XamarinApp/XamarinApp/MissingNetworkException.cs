using System;

namespace XamarinApp
{
    public class MissingNetworkException : Exception
    {
        public MissingNetworkException()
        {
        }

        public MissingNetworkException(string message)
        : base(message)
        {
        }

        public MissingNetworkException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}