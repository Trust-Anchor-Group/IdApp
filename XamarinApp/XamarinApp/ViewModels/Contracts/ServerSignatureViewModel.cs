using System;
using System.Globalization;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace XamarinApp.ViewModels.Contracts
{
    public class ServerSignatureViewModel : BaseViewModel
    {
        public ServerSignatureViewModel(Contract contract)
        {
            if (contract != null)
            {
                this.Provider = contract.Provider;
                this.Timestamp = contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture);
                this.Signature = Convert.ToBase64String(contract.ServerSignature.DigitalSignature);
            }
        }

        public static readonly BindableProperty ProviderProperty =
            BindableProperty.Create("Provider", typeof(string), typeof(ServerSignatureViewModel), default(string));

        public string Provider
        {
            get { return (string) GetValue(ProviderProperty); }
            set { SetValue(ProviderProperty, value); }
        }

        public static readonly BindableProperty TimestampProperty =
            BindableProperty.Create("Timestamp", typeof(string), typeof(ServerSignatureViewModel), default(string));

        public string Timestamp
        {
            get { return (string)GetValue(TimestampProperty); }
            set { SetValue(TimestampProperty, value); }
        }

        public static readonly BindableProperty SignatureProperty =
            BindableProperty.Create("Signature", typeof(string), typeof(ServerSignatureViewModel), default(string));

        public string Signature
        {
            get { return (string)GetValue(SignatureProperty); }
            set { SetValue(SignatureProperty, value); }
        }
    }
}