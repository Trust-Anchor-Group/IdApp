using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Models;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Contracts
{
    public class NewContractViewModel : BaseViewModel
    {
        private readonly SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory;
        private Contract template;
        private readonly INavigationService navigationService;
        private readonly IContractsService contractsService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly TagProfile tagProfile;

        private NewContractViewModel()
        {
            this.contractTypesPerCategory = new SortedDictionary<string, SortedDictionary<string, string>>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.VisibilityItems = new ObservableCollection<string>
            {
                AppResources.ContractVisibilityItem1,
                AppResources.ContractVisibilityItem2,
                AppResources.ContractVisibilityItem3,
                AppResources.ContractVisibilityItem4
            };
            this.ContractCategories = new ObservableCollection<string>();
            this.ContractTypes = new ObservableCollection<string>();
            this.ProposeCommand = new Command(_ => this.Propose(), _ => this.CanPropose());
            this.VisibilityItems = new ObservableCollection<string>();
            this.Roles = new ObservableCollection<RoleModel>();
            this.ContractRoles = new ObservableCollection<RoleModel>();
            this.ContractParameters = new ObservableCollection<ParameterModel>();
            this.ContractHumanReadableText = new ObservableCollection<string>();
        }

        public NewContractViewModel(Contract template)
            : this()
        {
            this.IsTemplate = true;
            this.template = template;
        }

        public NewContractViewModel(SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
            : this()
        {
            this.IsTemplate = false;
            this.contractTypesPerCategory = contractTypesPerCategory;
        }

        public static readonly BindableProperty IsTemplateProperty =
            BindableProperty.Create("IsTemplate", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool IsTemplate
        {
            get { return (bool)GetValue(IsTemplateProperty); }
            set { SetValue(IsTemplateProperty, value); }
        }

        public ObservableCollection<string> VisibilityItems { get; }

        public static readonly BindableProperty SelectedVisibilityItemProperty =
            BindableProperty.Create("SelectedVisibilityItem", typeof(string), typeof(NewContractViewModel), default(string));

        public string SelectedVisibilityItem
        {
            get { return (string)GetValue(SelectedVisibilityItemProperty); }
            set { SetValue(SelectedVisibilityItemProperty, value); }
        }

        public static readonly BindableProperty VisibilityIsEnabledProperty =
            BindableProperty.Create("VisibilityIsEnabled", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool VisibilityIsEnabled
        {
            get { return (bool)GetValue(VisibilityIsEnabledProperty); }
            set { SetValue(VisibilityIsEnabledProperty, value); }
        }

        public ObservableCollection<string> ContractCategories { get; }

        public static readonly BindableProperty SelectedContractCategoryProperty =
            BindableProperty.Create("SelectedContractCategory", typeof(string), typeof(NewContractViewModel), default(string));

        public string SelectedContractCategory
        {
            get { return (string)GetValue(SelectedContractCategoryProperty); }
            set { SetValue(SelectedContractCategoryProperty, value); }
        }

        public ObservableCollection<string> ContractTypes { get; }

        public static readonly BindableProperty SelectedContractTypeProperty =
            BindableProperty.Create("SelectedContractType", typeof(string), typeof(NewContractViewModel), default(string));

        public string SelectedContractType
        {
            get { return (string)GetValue(SelectedContractTypeProperty); }
            set { SetValue(SelectedContractTypeProperty, value); }
        }

        public ICommand ProposeCommand { get; }

        public static readonly BindableProperty PinProperty =
            BindableProperty.Create("Pin", typeof(string), typeof(NewContractViewModel), default(string));

        public string Pin
        {
            get { return (string)GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        public ObservableCollection<RoleModel> Roles { get; }

        public static readonly BindableProperty SelectedRoleProperty =
            BindableProperty.Create("SelectedRole", typeof(RoleModel), typeof(NewContractViewModel), default(RoleModel));

        public RoleModel SelectedRole
        {
            get { return (RoleModel) GetValue(SelectedRoleProperty); }
            set { SetValue(SelectedRoleProperty, value); }
        }

        public ObservableCollection<RoleModel> ContractRoles { get; }

        public ObservableCollection<ParameterModel> ContractParameters { get; }

        public ObservableCollection<string> ContractHumanReadableText { get; }

        private void Propose()
        {

        }

        private bool CanPropose()
        {
            return false;
        }
    }
}