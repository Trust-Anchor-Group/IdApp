using Waher.Networking.XMPP;

namespace IdApp.Extensions
{
    public static class XmppStateExtensions
    {
        public static string ToDisplayText(this XmppState state, string domain)
        {
            switch (state)
            {
                case XmppState.Authenticating:
                    return AppResources.XmppState_Authenticating;

                case XmppState.Binding:
                    return AppResources.XmppState_Binding;

                case XmppState.Connected:
                    return !string.IsNullOrWhiteSpace(domain)
                        ? string.Format(AppResources.XmppState_ConnectedTo, domain)
                        : AppResources.XmppState_Connected;

                case XmppState.Connecting:
                    return AppResources.XmppState_Connecting;

                case XmppState.Error:
                    return AppResources.XmppState_Error;

                case XmppState.FetchingRoster:
                    return AppResources.XmppState_FetchingRoster;

                case XmppState.Registering:
                    return AppResources.XmppState_Registering;

                case XmppState.RequestingSession:
                    return AppResources.XmppState_RequestingSession;

                case XmppState.SettingPresence:
                    return AppResources.XmppState_SettingPresence;

                case XmppState.StartingEncryption:
                    return AppResources.XmppState_StartingEncryption;

                case XmppState.StreamNegotiation:
                    return AppResources.XmppState_StreamNegotiation;

                case XmppState.StreamOpened:
                    return AppResources.XmppState_StreamOpened;

                default:
                    return AppResources.XmppState_Offline;
            }
        }
    }
}