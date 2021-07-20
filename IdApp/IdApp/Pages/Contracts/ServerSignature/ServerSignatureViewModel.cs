using System;
using System.Globalization;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.Pages.Contracts.ServerSignature
{
    /// <summary>
    /// The view model to bind to for when displaying server signatures.
    /// </summary>
    public class ServerSignatureViewModel : BaseViewModel
    {
        private readonly INavigationService navigationService;
        private Contract contract;

        /// <summary>
        /// Creates an instance of the <see cref="ServerSignatureViewModel"/> class.
        /// </summary>
        public ServerSignatureViewModel()
            : this(null)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="ServerSignatureViewModel"/> class.
        /// For unit tests.
        /// <param name="navigationService">The navigation service.</param>
        /// </summary>
        protected internal ServerSignatureViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService ?? Types.Instantiate<INavigationService>(false);
        }

        /// <inheritdoc/>
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