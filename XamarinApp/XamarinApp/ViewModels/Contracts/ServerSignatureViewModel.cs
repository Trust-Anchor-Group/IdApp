using System;
using System.Globalization;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Contracts
{
    public class ServerSignatureViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;
        private readonly Contract contract;

        public ServerSignatureViewModel(Contract contract)
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            if (contract != null)
            {
                this.Provider = contract.Provider;
                this.Timestamp = this.contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture);
                this.Signature = Convert.ToBase64String(this.contract.ServerSignature.DigitalSignature);
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