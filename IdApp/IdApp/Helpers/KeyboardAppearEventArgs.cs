using System;

namespace IdApp.Helpers
{
    public class KeyboardAppearEventArgs : EventArgs
    {
        public float KeyboardSize { get; set; }

        public KeyboardAppearEventArgs()
        {
        }
    }
}