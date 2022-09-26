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
        protected override async Task OnInitialize()
        {
            await base.OnInitialize();
            
            if (this.NavigationService.TryPopArgs(out ServerSignatureNavigationArgs args))
                this.contract = args.Contract;

			this.AssignProperties();
        }

        #region Properties

        /// <summary>
        /// See <see cref="Provider"/>
        /// </summary>
        public static readonly BindableProperty ProviderProperty =
            BindableProperty.Create(nameof(Provider), typeof(string), typeof(ServerSignatureViewModel), default(string));

        /// <summary>
        /// The provider of the server signature contract.
        /// </summary>
        public string Provider
        {
            get => (string)this.GetValue(ProviderProperty);
            set => this.SetValue(ProviderProperty, value);
        }

        /// <summary>
        /// See <see cref="Timestamp"/>
        /// </summary>
        public static readonly BindableProperty TimestampProperty =
            BindableProperty.Create(nameof(Timestamp), typeof(string), typeof(ServerSignatureViewModel), default(string));

        /// <summary>
        /// The time stamp of the server signature contract.
        /// </summary>
        public string Timestamp
        {
            get => (string)this.GetValue(TimestampProperty);
            set => this.SetValue(TimestampProperty, value);
        }

        /// <summary>
        /// See <see cref="Signature"/>
        /// </summary>
        public static readonly BindableProperty SignatureProperty =
            BindableProperty.Create(nameof(Signature), typeof(string), typeof(ServerSignatureViewModel), default(string));

        /// <summary>
        /// The signature of the server signature contract.
        /// </summary>
        public string Signature
        {
            get => (string)this.GetValue(SignatureProperty);
            set => this.SetValue(SignatureProperty, value);
        }

        #endregion

        private void AssignProperties()
        {
            if (this.contract is not null)
            {
                this.Provider = this.contract.Provider;
                this.Timestamp = this.contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture);
                this.Signature = Convert.ToBase64String(this.contract.ServerSignature.DigitalSignature);
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
