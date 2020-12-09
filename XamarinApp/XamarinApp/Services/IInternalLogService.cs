using Waher.Networking.XMPP;

namespace XamarinApp.Services
{
    internal interface IInternalLogService : ILogService
    {
        void RegisterEventSink(XmppClient client, string logJid);
        void UnregisterEventSink();
    }
}