using System;

namespace IdApp.Views
{
    public sealed class CodeScannedEventArgs : EventArgs
    {
        public CodeScannedEventArgs(string code)
        {
            Code = code;
        }

        public string Code { get; }
    }
}