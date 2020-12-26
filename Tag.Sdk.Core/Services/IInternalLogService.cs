using Waher.Networking.XMPP;

namespace Tag.Sdk.Core.Services
{
    internal interface IInternalLogService : ILogService
    {
        void RegisterEventSink(XmppClient client, string logJid);
        void UnregisterEventSink();
    }
}