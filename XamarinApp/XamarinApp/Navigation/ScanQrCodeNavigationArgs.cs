using Tag.Sdk.Core.Services;

namespace XamarinApp.Navigation
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