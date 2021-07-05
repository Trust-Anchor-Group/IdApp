using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="XmppState"/> enum.
    /// </summary>
    public static class XmppStateExtensions
    {
        /// <summary>
        /// Returns a color matching the current connection state.
        /// </summary>
        /// <param name="State">Connection state.</param>
        /// <returns>Color</returns>
        public static Color ToColor(this XmppState State)
        {
            switch (State)
            {
                case XmppState.Error:
                case XmppState.Offline:
                    return Color.Red;

                case XmppState.Authenticating:
                case XmppState.Connecting:
                case XmppState.Registering:
                case XmppState.StartingEncryption:
                case XmppState.StreamNegotiation:
                case XmppState.StreamOpened:
                    return Color.Yellow;

                case XmppState.Binding:
                case XmppState.FetchingRoster:
                case XmppState.RequestingSession:
                case XmppState.SettingPresence:
                    return Blend(Color.Yellow, Color.LightGreen, 0.5);

                case XmppState.Connected:
                    return Color.LightGreen;

                default:
                    return Color.Gray;
            }
        }

        /// <summary>
        /// Blends two colors.
        /// </summary>
        /// <param name="Color1">Color 1</param>
        /// <param name="Color2">Color 2</param>
        /// <param name="p">Blending coefficient (0-1).</param>
        /// <returns>Blended color.</returns>
        public static Color Blend(Color Color1, Color Color2, double p)
		{
            int R = (int)(Color1.R * (1 - p) + Color2.R * p + 0.5);
            int G = (int)(Color1.G * (1 - p) + Color2.G * p + 0.5);
            int B = (int)(Color1.B * (1 - p) + Color2.B * p + 0.5);
            int A = (int)(Color1.A * (1 - p) + Color2.A * p + 0.5);

            return new Color(R, G, B, A);
        }

        /// <summary>
        /// Converts the state to a localized string.
        /// </summary>
        /// <param name="State">The state to convert.</param>
        /// <returns>Textual representation of an XMPP connection state.</returns>
        public static string ToDisplayText(this XmppState State)
        {
            switch (State)
            {
                case XmppState.Authenticating:
                    return AppResources.XmppState_Authenticating;

                case XmppState.Binding:
                    return AppResources.XmppState_Binding;

                case XmppState.Connected:
                    return AppResources.XmppState_Connected;

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


        /// <summary>
        /// Converts the state to a localized string.
        /// </summary>
        /// <param name="State">The state to convert.</param>
        /// <returns></returns>
        public static string ToDisplayText(this IdentityState State)
        {
            switch (State)
            {
                case IdentityState.Approved:
                    return AppResources.IdentityState_Approved;

                case IdentityState.Compromised:
                    return AppResources.IdentityState_Compromized;

                case IdentityState.Created:
                    return AppResources.IdentityState_Created;

                case IdentityState.Obsoleted:
                    return AppResources.IdentityState_Obsoleted;

                case IdentityState.Rejected:
                    return AppResources.IdentityState_Rejected;

                default:
                    return string.Empty;
            }
        }

    }
}