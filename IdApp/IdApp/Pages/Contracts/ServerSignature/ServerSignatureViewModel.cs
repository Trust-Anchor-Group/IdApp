using System;
using System.Globalization;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Pages.Contracts.ServerSignature
{
    /// <summary>
    /// The view model to bind to for when displaying server signatures.
    /// </summary>
    public class ServerSignatureViewModel : BaseViewModel
    {
        private Contract contract;

        /// <summary>
        /// Creates an instance of the <see cref="ServerSignatureViewModel"/> class.
        /// </summary>
        protected internal ServerSignatureViewModel()
        {
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();
            
            if (this.NavigationService.TryPopArgs(out ServerSignatureNavigationArgs args))
                this.contract = args.Contract;
            
            AssignProperties();
        }

        #region Properties

        /// <summary>
        /// See <see cref="Provider"/>
        /// </summary>
        public static readonly BindableProperty ProviderProperty =
            BindableProperty.Create("Provider", typeof(string), typeof(ServerSignatureViewModel), default(string));

        /// <summary>
        /// The provider of the server signature contract.
        /// </summary>
        public string Provider
        {
            get { return (string) GetValue(ProviderProperty); }
            set { SetValue(ProviderProperty, value); }
        }

        /// <summary>
        /// See <see cref="Timestamp"/>
        /// </summary>
        public static readonly BindableProperty TimestampProperty =
            BindableProperty.Create("Timestamp", typeof(string), typeof(ServerSignatureViewModel), default(string));

        /// <summary>
        /// The time stamp of the server signature contract.
        /// </summary>
        public string Timestamp
        {
            get { return (string)GetValue(TimestampProperty); }
            set { SetValue(TimestampProperty, value); }
        }

        /// <summary>
        /// See <see cref="Signature"/>
        /// </summary>
        public static readonly BindableProperty SignatureProperty =
            BindableProperty.Create("Signature", typeof(string), typeof(ServerSignatureViewModel), default(string));

        /// <summary>
        /// The signature of the server signature contract.
        /// </summary>
        public string Signature
        {
            get { return (string)GetValue(SignatureProperty); }
            set { SetValue(SignatureProperty, value); }
        }

        #endregion

        private void AssignProperties()
        {
            if (!(contract is null))
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