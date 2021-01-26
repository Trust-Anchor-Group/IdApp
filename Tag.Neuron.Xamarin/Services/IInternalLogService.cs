using Waher.Networking.XMPP;

namespace Tag.Neuron.Xamarin.Services
{
    internal interface IInternalLogService : ILogService
    {
        void RegisterEventSink(XmppClient client, string logJid);
        void UnRegisterEventSink();
    }
}