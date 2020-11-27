using System;
using System.ComponentModel;
using Xamarin.Forms;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
	[DesignTimeVisible(true)]
	public partial class ServerSignaturePage
	{
		private readonly Page owner;
		private readonly Contract contract;
        private readonly INavigationService navigationService;

		public ServerSignaturePage(Page owner, Contract contract)
		{
			this.owner = owner;
			this.contract = contract;
			this.BindingContext = this;
            this.navigationService = DependencyService.Resolve<INavigationService>();
			InitializeComponent();
		}

		public string Provider => this.contract.Provider;
		public string Timestamp => this.contract.ServerSignature.Timestamp.ToString();
		public string Signature => Convert.ToBase64String(this.contract.ServerSignature.DigitalSignature);

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
