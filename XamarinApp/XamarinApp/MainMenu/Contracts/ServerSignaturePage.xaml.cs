using System;
using System.ComponentModel;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.MainMenu.Contracts
{
	// Learn more about making custom code visible in the Xamarin.Forms previewer
	// by visiting https://aka.ms/xamarinforms-previewer
	[DesignTimeVisible(true)]
	public partial class ServerSignaturePage : ContentPage, IBackButton
	{
		private readonly Page owner;
		private readonly Contract contract;

		public ServerSignaturePage(Page Owner, Contract Contract)
		{
			this.owner = Owner;
			this.contract = Contract;
			this.BindingContext = this;
			InitializeComponent();
		}

		private void BackButton_Clicked(object sender, EventArgs e)
		{
			App.ShowPage(this.owner, true);
		}

		public string Provider => this.contract.Provider;
		public string Timestamp => this.contract.ServerSignature.Timestamp.ToString();
		public string Signature => Convert.ToBase64String(this.contract.ServerSignature.DigitalSignature);

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}
	}
}
