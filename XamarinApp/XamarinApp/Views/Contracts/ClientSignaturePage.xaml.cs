using System;
using System.ComponentModel;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
	[DesignTimeVisible(true)]
	public partial class ClientSignaturePage
	{
		private readonly ClientSignature clientSignature;
		private readonly LegalIdentity identity;
        private readonly INavigationService navigationService;

		public ClientSignaturePage(ClientSignature signature, LegalIdentity identity)
		{
			this.clientSignature = signature;
			this.identity = identity;
			this.BindingContext = this;
            this.navigationService = DependencyService.Resolve<INavigationService>();
			InitializeComponent();
		}

		public DateTime Created => this.identity.Created;
		public DateTime? Updated => CheckMin(this.identity.Updated);
		public string LegalId => this.identity.Id;
		public string State => this.identity.State.ToString();
		public DateTime? From => CheckMin(this.identity.From);
		public DateTime? To => CheckMin(this.identity.To);
		public string FirstName => this.identity["FIRST"];
		public string MiddleNames => this.identity["MIDDLE"];
		public string LastNames => this.identity["LAST"];
		public string PNr => this.identity["PNR"];
		public string Address => this.identity["ADDR"];
		public string Address2 => this.identity["ADDR2"];
		public string PostalCode => this.identity["ZIP"];
		public string Area => this.identity["AREA"];
		public string City => this.identity["CITY"];
		public string Region => this.identity["REGION"];
		public string CountryCode => this.identity["COUNTRY"];
		public string Country => ISO_3166_1.ToName(this.CountryCode);
		public bool IsApproved => this.identity.State == IdentityState.Approved;

		public string Role => this.clientSignature.Role;
		public string Timestamp => this.clientSignature.Timestamp.ToString();
		public string Transferable => this.clientSignature.Transferable ? "✔" : "✗";
		public string BareJid => this.clientSignature.BareJid;
		public string Signature => Convert.ToBase64String(this.clientSignature.DigitalSignature);

		private static DateTime? CheckMin(DateTime? tp)
		{
			if (!tp.HasValue || tp.Value == DateTime.MinValue)
				return null;
			else
				return tp;
		}

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PopAsync();
        }

		protected override bool OnBackButtonPressed()
        {
			this.BackButton_Clicked(this.BackButton, EventArgs.Empty);
            return true;
        }
    }
}
