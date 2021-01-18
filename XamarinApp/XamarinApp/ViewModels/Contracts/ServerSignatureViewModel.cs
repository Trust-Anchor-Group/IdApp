using System;
using System.Globalization;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Navigation;

namespace XamarinApp.ViewModels.Contracts
{
    public class ServerSignatureViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;
        private Contract contract;

        public ServerSignatureViewModel()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            if (this.navigationService.TryPopArgs(out ServerSignatureNavigationArgs args))
            {
                this.contract = args.Contract;
            }
            AssignProperties();
        }

        #region Properties

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

        #endregion

        private void AssignProperties()
        {
            if (contract != null)
            {
                this.Provider = contract.Provider;
                this.Timestamp = contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture);
                this.Signature = Convert.ToBase64String(contract.ServerSignature.DigitalSignature);
            }
            else
            {
                this.Provider = Constants.NotAvailableValue;
                this.Timestamp = Constants.NotAvailableValue;
                this.Signature = Constants.NotAvailableValue;
            }
        }
    }
}