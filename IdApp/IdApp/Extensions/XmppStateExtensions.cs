using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;

namespace IdApp.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="XmppState"/> enum.
    /// </summary>
    public static class XmppStateExtensions
    {
        /// <summary>
        /// Converts the state to a localized string.
        /// </summary>
        /// <param name="state">The state to convert.</param>
        /// <param name="TagProfile">TAG profile reference.</param>
        /// <returns></returns>
        public static string ToDisplayText(this XmppState state, ITagProfile TagProfile)
        {
            switch (state)
            {
                case XmppState.Authenticating:
                    return AppResources.XmppState_Authenticating;

                case XmppState.Binding:
                    return AppResources.XmppState_Binding;

                case XmppState.Connected:
                    return TagProfile is null ? AppResources.XmppState_Connected : TagProfile.Account + "@" + TagProfile.Domain;

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