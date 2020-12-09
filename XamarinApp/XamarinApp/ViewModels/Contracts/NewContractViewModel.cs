using System.Collections.Generic;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Contracts
{
    public class NewContractViewModel : BaseViewModel
    {
        private readonly SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory;
        private readonly INavigationService navigationService;
        private readonly IContractsService contractsService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly TagProfile tagProfile;
        private readonly List<string> contractTypes;
        private Contract template;
        private string contractCategory;
        private string contractType;
        private string templateId;
        private string role;


        private NewContractViewModel()
        {
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.contractTypes = new List<string>();
        }

        public NewContractViewModel(Contract template)
            : this()
        {
            this.contractTypes = new List<string>();
        }

        public NewContractViewModel(SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
            : this()
        {
        }
    }
}