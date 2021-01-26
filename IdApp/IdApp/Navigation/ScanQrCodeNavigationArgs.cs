using Tag.Neuron.Xamarin.Services;

namespace IdApp.Navigation
{
    public class ScanQrCodeNavigationArgs : NavigationArgs
    {
        public ScanQrCodeNavigationArgs(string commandName)
        {
            this.CommandName = commandName;
        }

        public string CommandName { get; }
    }
}