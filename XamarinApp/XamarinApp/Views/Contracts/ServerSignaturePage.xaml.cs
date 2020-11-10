using System;
using System.ComponentModel;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
	[DesignTimeVisible(true)]
	public partial class ServerSignaturePage : ContentPage, IBackButton
    {
        private readonly ITagService tagService;
		private readonly Page owner;
		private readonly Contract contract;

		public ServerSignaturePage(Page Owner, Contract Contract)
		{
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.owner = Owner;
			this.contract = Contract;
			this.BindingContext = this;
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
