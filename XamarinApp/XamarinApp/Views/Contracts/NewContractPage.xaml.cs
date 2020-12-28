using System.Collections.Generic;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.ViewModels.Contracts;

namespace XamarinApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage
	{
        private readonly INavigationService navigationService;

		public NewContractPage(SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
		    : this(null, contractTypesPerCategory)
		{
		}

		public NewContractPage(Contract template)
		    : this(template, null)
		{
		}

		private NewContractPage(Contract contractTemplate, SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
			if (contractTemplate != null)
            {
                this.ViewModel = new NewContractViewModel(contractTemplate);
            }
            else
            {
                this.ViewModel = new NewContractViewModel(contractTypesPerCategory);
            }
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
	}
}