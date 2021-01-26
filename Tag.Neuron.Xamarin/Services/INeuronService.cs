using System;
using System.Reflection;
using System.Threading.Tasks;
using Waher.Networking.XMPP;

namespace Tag.Neuron.Xamarin.Services
{
    public interface INeuronService : ILoadableService, IDisposable
    {
        Task<bool> WaitForConnectedState(TimeSpan timeout);
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        Task<(bool succeeded, string errorMessage)> TryConnect(string domain, string hostName, int portNumber, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);
        Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, string hostName, int portNumber, string userName, string password, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);
        Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, string hostName, int portNumber, string userName, string password, string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);

        bool IsOnline { get; }
        XmppState State { get; }
        string BareJId { get; }
        string LatestError { get; }
        string LatestConnectionError { get; }
        INeuronContracts Contracts { get; }
        INeuronChats Chats { get; }

        Task<bool> DiscoverServices(XmppClient client = null);

        string CommsDumpAsHtml();
    }
}