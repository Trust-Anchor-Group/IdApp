using System;
using System.ComponentModel;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(true)]
	public partial class ClientSignaturePage : IBackButton
    {
        private readonly ITagService tagService;
		private readonly Page owner;
		private readonly ClientSignature clientSignature;
		private readonly LegalIdentity identity;

		public ClientSignaturePage(Page Owner, ClientSignature Signature, LegalIdentity Identity)
		{
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.owner = Owner;
			this.clientSignature = Signature;
			this.identity = Identity;
			this.BindingContext = this;
		}

		private void BackButton_Clicked(object sender, EventArgs e)
		{
			App.ShowPage(this.owner, true);
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
		public string Country => this.identity["COUNTRY"];
		public bool IsApproved => this.identity.State == IdentityState.Approved;

		public string Role => this.clientSignature.Role;
		public string Timestamp => this.clientSignature.Timestamp.ToString();
		public string Transferable => this.clientSignature.Transferable ? "✔" : "✗";
		public string BareJid => this.clientSignature.BareJid;
		public string Signature => Convert.ToBase64String(this.clientSignature.DigitalSignature);

		private static DateTime? CheckMin(DateTime? TP)
		{
			if (!TP.HasValue || TP.Value == DateTime.MinValue)
				return null;
			else
				return TP;
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, EventArgs.Empty);
			return true;
		}

	}
}
