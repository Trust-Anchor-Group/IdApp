using IdApp.Models;
using IdApp.Navigation;
using IdApp.Services;
using IdApp.Views.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Extensions;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.ViewModels.Contracts
{
    public class NewContractViewModel : BaseViewModel
    {
        private SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory;
        private Contract contractTemplate;
        private readonly ILogService logService;
        private readonly INeuronService neuronService;
        private readonly INetworkService networkService;
        private readonly INavigationService navigationService;
        private readonly IUiDispatcher uiDispatcher;
        private readonly ISettingsService settingsService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly ITagProfile tagProfile;
        private string contractTemplateId;
        private bool saveState;

        public NewContractViewModel()
        {
            this.contractTypesPerCategory = new SortedDictionary<string, SortedDictionary<string, string>>();
            this.logService = DependencyService.Resolve<ILogService>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.networkService = DependencyService.Resolve<INetworkService>();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.settingsService = DependencyService.Resolve<ISettingsService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
            this.tagProfile = DependencyService.Resolve<ITagProfile>();
            this.ContractVisibilityItems = new ObservableCollection<ContractVisibilityModel>();
            this.ContractCategories = new ObservableCollection<string>();
            this.ContractTypes = new ObservableCollection<string>();
            this.ProposeCommand = new Command(async _ => await this.Propose(), _ => this.CanPropose());
            this.AddPartCommand = new Command(async _ => await this.AddPart());
            this.ParameterChangedCommand = new Command(EditParameter);
            this.ContractRoles = new ObservableCollection<RoleModel>();
            this.ContractParameters = new ObservableCollection<ParameterModel>();
            this.ContractHumanReadableText = new ObservableCollection<string>();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            if (this.navigationService.TryPopArgs(out NewContractNavigationArgs args))
            {
                this.contractTemplate = args.Contract;
                this.contractTypesPerCategory = args.ContractTypesPerCategory;
            }
            this.IsTemplate = this.contractTypesPerCategory != null;

            if (this.contractTypesPerCategory != null && this.contractTypesPerCategory.Count > 0)
            {
                foreach (string contractType in this.contractTypesPerCategory.Keys)
                {
                    this.ContractCategories.Add(contractType);
                }
            }

            this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.CreatorAndParts, AppResources.ContractVisibility_CreatorAndParts));
            this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.DomainAndParts, AppResources.ContractVisibility_DomainAndParts));
            this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.Public, AppResources.ContractVisibility_Public));
            this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.PublicSearchable, AppResources.ContractVisibility_PublicSearchable));

            this.UsePin = this.tagProfile.UsePin;
        }

        protected override async Task DoUnbind()
        {
            this.ContractCategories.Clear();
            this.ContractTypes.Clear();
            this.ContractRoles.Clear();
            this.ContractVisibilityItems.Clear();
            this.ContractHumanReadableText.Clear();
            this.ContractParameters.Clear();
            await base.DoUnbind();
        }

        protected override async Task DoSaveState()
        {
            await base.DoSaveState();
            this.settingsService.SaveState(GetSettingsKey(nameof(SelectedContractCategory)), this.SelectedContractCategory);
            this.settingsService.SaveState(GetSettingsKey(nameof(SelectedContractType)), this.SelectedContractType);
            if (this.SelectedContractVisibilityItem != null)
            {
                this.settingsService.SaveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)), (ContractVisibility?)this.SelectedContractVisibilityItem.Visibility);
            }
        }

        protected override async Task DoRestoreState()
        {
            if (this.saveState)
            {
                this.SelectedContractCategory = this.settingsService.RestoreState<string>(GetSettingsKey(nameof(SelectedContractCategory)));
                this.SelectedContractType = this.settingsService.RestoreState<string>(GetSettingsKey(nameof(SelectedContractType)));
                ContractVisibility? visibility = this.settingsService.RestoreState<ContractVisibility?>(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
                if (visibility != null)
                {
                    this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == visibility);
                }
            }
            await base.DoRestoreState();
        }

        private void DeleteState()
        {
            this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedContractCategory)));
            this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedContractType)));
            this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
        }

        #region Properties

        public ICommand ProposeCommand { get; }

        public ICommand AddPartCommand { get; }

        public ICommand ParameterChangedCommand { get; }

        public static readonly BindableProperty IsTemplateProperty =
            BindableProperty.Create("IsTemplate", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool IsTemplate
        {
            get { return (bool)GetValue(IsTemplateProperty); }
            set { SetValue(IsTemplateProperty, value); }
        }

        public ObservableCollection<ContractVisibilityModel> ContractVisibilityItems { get; }

        public static readonly BindableProperty SelectedContractVisibilityItemProperty =
            BindableProperty.Create("SelectedContractVisibilityItem", typeof(ContractVisibilityModel), typeof(NewContractViewModel), default(ContractVisibilityModel));

        public ContractVisibilityModel SelectedContractVisibilityItem
        {
            get { return (ContractVisibilityModel)GetValue(SelectedContractVisibilityItemProperty); }
            set { SetValue(SelectedContractVisibilityItemProperty, value); }
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
            BindableProperty.Create("SelectedContractCategory", typeof(string), typeof(NewContractViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                NewContractViewModel viewModel = (NewContractViewModel)b;
                viewModel.UpdateContractTypes();
            });

        public string SelectedContractCategory
        {
            get { return (string)GetValue(SelectedContractCategoryProperty); }
            set { SetValue(SelectedContractCategoryProperty, value); }
        }

        public ObservableCollection<string> ContractTypes { get; }

        public static readonly BindableProperty HasContractTypesProperty =
            BindableProperty.Create("HasContractTypes", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool HasContractTypes
        {
            get { return (bool)GetValue(HasContractTypesProperty); }
            set { SetValue(HasContractTypesProperty, value); }
        }

        public static readonly BindableProperty SelectedContractTypeProperty =
            BindableProperty.Create("SelectedContractType", typeof(string), typeof(NewContractViewModel), default(string), propertyChanged: async (b, oldValue, newValue) =>
            {
                NewContractViewModel viewModel = (NewContractViewModel)b;
                await viewModel.UpdateContractTemplate();
            });

        public string SelectedContractType
        {
            get { return (string)GetValue(SelectedContractTypeProperty); }
            set { SetValue(SelectedContractTypeProperty, value); }
        }

        public static readonly BindableProperty PinProperty =
            BindableProperty.Create("Pin", typeof(string), typeof(NewContractViewModel), default(string));

        public string Pin
        {
            get { return (string)GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        public static readonly BindableProperty SelectedRoleProperty =
            BindableProperty.Create("SelectedRole", typeof(RoleModel), typeof(NewContractViewModel), default(RoleModel), propertyChanged: (b, oldValue, newValue) =>
            {
                NewContractViewModel viewModel = (NewContractViewModel)b;
                viewModel.RemoveRole((RoleModel)oldValue);
                viewModel.AddRole((RoleModel)newValue);
            });

        public RoleModel SelectedRole
        {
            get { return (RoleModel)GetValue(SelectedRoleProperty); }
            set { SetValue(SelectedRoleProperty, value); }
        }

        public ObservableCollection<RoleModel> ContractRoles { get; }

        public ObservableCollection<ParameterModel> ContractParameters { get; }

        public ObservableCollection<string> ContractHumanReadableText { get; }

        public static readonly BindableProperty UsePinProperty =
            BindableProperty.Create("UsePin", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool UsePin
        {
            get { return (bool)GetValue(UsePinProperty); }
            set { SetValue(UsePinProperty, value); }
        }

        public static readonly BindableProperty HasTemplateProperty =
            BindableProperty.Create("HasTemplate", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool HasTemplate
        {
            get { return (bool)GetValue(HasTemplateProperty); }
            set { SetValue(HasTemplateProperty, value); }
        }

        public static readonly BindableProperty HasRolesProperty =
            BindableProperty.Create("HasRoles", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool HasRoles
        {
            get { return (bool)GetValue(HasRolesProperty); }
            set { SetValue(HasRolesProperty, value); }
        }

        public static readonly BindableProperty HasParametersProperty =
            BindableProperty.Create("HasParameters", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool HasParameters
        {
            get { return (bool)GetValue(HasParametersProperty); }
            set { SetValue(HasParametersProperty, value); }
        }

        public static readonly BindableProperty HasHumanReadableTextProperty =
            BindableProperty.Create("HasHumanReadableText", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool HasHumanReadableText
        {
            get { return (bool)GetValue(HasHumanReadableTextProperty); }
            set { SetValue(HasHumanReadableTextProperty, value); }
        }

        public static readonly BindableProperty CanAddPartsProperty =
            BindableProperty.Create("CanAddParts", typeof(bool), typeof(NewContractViewModel), default(bool));

        public bool CanAddParts
        {
            get { return (bool)GetValue(CanAddPartsProperty); }
            set { SetValue(CanAddPartsProperty, value); }
        }

        #endregion

        private void UpdateContractTypes()
        {
            this.ContractTypes.Clear();

            if (!string.IsNullOrWhiteSpace(this.SelectedContractCategory) &&
                this.contractTypesPerCategory != null &&
                this.contractTypesPerCategory.TryGetValue(this.SelectedContractCategory, out SortedDictionary<string, string> idsPerType))
            {
                foreach (KeyValuePair<string, string> id in idsPerType)
                {
                    this.ContractTypes.Add(id.Key);
                }
            }

            this.HasContractTypes = this.ContractTypes.Count > 0;
        }

        private async Task UpdateContractTemplate()
        {
            this.ClearTemplate();

            if (!string.IsNullOrWhiteSpace(this.SelectedContractType) &&
                this.contractTypesPerCategory != null &&
                this.contractTypesPerCategory.TryGetValue(this.SelectedContractCategory, out SortedDictionary<string, string> idsPerType) &&
                idsPerType.TryGetValue(this.SelectedContractType, out string templateId))
            {
                this.contractTemplateId = templateId;
                await this.LoadTemplate();
            }
        }

        private void ClearTemplate()
        {
            this.contractTemplate = null;
            this.ContractParameters.Clear();
            this.ContractRoles.Clear();
            this.ContractHumanReadableText.Clear();
            this.UsePin = false;
            this.HasTemplate = false;
            this.HasRoles = this.ContractRoles.Count > 0;
            this.HasParameters = this.ContractParameters.Count > 0;
            this.HasHumanReadableText = this.ContractHumanReadableText.Count > 0;
            this.CanAddParts = false;
            this.ProposeCommand.ChangeCanExecute();
        }

        private async Task LoadTemplate()
        {
            try
            {
                (bool succeeded, Contract retrievedContract) = await this.networkService.TryRequest(() => this.neuronService.Contracts.GetContract(this.contractTemplateId));
                if (!succeeded)
                    return;

                this.contractTemplate = retrievedContract;
                this.LoadTemplateParts();
                this.HasTemplate = true;
                this.HasRoles = this.ContractRoles.Count > 0;
                this.HasParameters = this.ContractParameters.Count > 0;
                this.HasHumanReadableText = this.ContractHumanReadableText.Count > 0;
                this.ProposeCommand.ChangeCanExecute();
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex, new KeyValuePair<string, string>("Method", "GetContract"), new KeyValuePair<string, string>("ContractId", this.contractTemplateId));
                ClearTemplate();
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private void LoadTemplateParts()
        {
            foreach (Role role in this.contractTemplate.Roles)
            {
                this.ContractRoles.Add(new RoleModel(role.Name));
                if (role.MinCount > 0)
                {
                    this.CanAddParts = true;
                }
                // TODO: replace this with a data template selector
                //Populate(this.Roles, role.ToXamarinForms(contract.DefaultLanguage, contract));
            }

            foreach (Parameter parameter in contractTemplate.Parameters)
            {
                this.ContractParameters.Add(new ParameterModel(parameter.Name, FilterDefaultValues(parameter.Name)));
                // TODO: replace this with a data template selector
                //Populate(Parameters, parameter.ToXamarinForms(contract.DefaultLanguage, contract));
            }
        }

        private void LoadHumanReadableText()
        {
            this.ContractHumanReadableText.Clear();

            if (this.contractTemplate != null)
            {
                // TODO: replace this with a data template selector
                //Populate(this.HumanReadableText, this.template.ToXamarinForms(this.template.DefaultLanguage));
            }
        }

        private static string FilterDefaultValues(string s)
        {
            foreach (char ch in s)
            {
                if (char.ToUpper(ch) != ch)
                    return s;
            }

            return string.Empty;
        }

        private async Task Propose()
        {
            try
            {
                List<Part> parts = new List<Part>();
                // TODO:fix this code.
#if NEEDS_FIXING
                string role = string.Empty;
                int state = 0;
                int nr = 0;
                int min = 0;
                int max = 0;
                foreach (RoleModel view in this.ContractRoles)
                {
                    switch (state)
                    {
                        case 0:
                            if (view is Label label && !string.IsNullOrEmpty(label.StyleId))
                            {
                                role = label.StyleId;
                                state++;
                                nr = min = max = 0;

                                Role r = this.contractTemplate.Roles.FirstOrDefault(x => x.Name == role);
                                if (r != null)
                                {
                                    min = r.MinCount;
                                    max = r.MaxCount;
                                }
                            }
                            break;

                        case 1:
                            if (view is Button)
                            {
                                if (nr < min)
                                {
                                    await this.navigationService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.TheContractRequiresAtLeast_AddMoreParts, min, role));
                                    return;
                                }

                                if (nr > min)
                                {
                                    await this.navigationService.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.TheContractRequiresAtMost_RemoveParts, max, role));
                                    return;
                                }

                                state--;
                                role = string.Empty;
                            }
                            else if (view is Label label2 && !string.IsNullOrEmpty(role))
                            {
                                parts.Add(new Part()
                                {
                                    Role = role,
                                    LegalId = label2.StyleId
                                });

                                nr++;
                            }
                            break;
                    }
                }
#endif

                if (this.ContractParameters.Any(x => !x.IsValid))
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.YourContractContainsErrors);
                    return;
                }

                this.contractTemplate.PartsMode = ContractParts.Open;

                if (this.SelectedContractVisibilityItem == null)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.ContractVisibilityMustBeSelected);
                    return;
                }

                this.contractTemplate.Visibility = this.SelectedContractVisibilityItem.Visibility;

                if (this.SelectedRole == null)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.ContractRoleMustBeSelected);
                    return;
                }

                if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
                    return;
                }

                (bool createSucceeded, Contract createdContract) = await this.networkService.TryRequest(() => 
                    this.neuronService.Contracts.CreateContract(
                        this.contractTemplateId,
                        parts.ToArray(),
                        this.contractTemplate.Parameters,
                        this.contractTemplate.Visibility,
                        ContractParts.ExplicitlyDefined,
                        this.contractTemplate.Duration,
                        this.contractTemplate.ArchiveRequired,
                        this.contractTemplate.ArchiveOptional,
                        null,
                        null,
                        false));

                Contract signedContract = null;
                if (createSucceeded)
                {
                    (bool signSucceeded, Contract contract) = await this.networkService.TryRequest(() => this.neuronService.Contracts.SignContract(createdContract, this.SelectedRole.Name, false));
                    if (signSucceeded)
                    {
                        signedContract = contract;
                    }
                }

                if (signedContract != null)
                {
                    await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(createdContract, false));
                }
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private bool CanPropose()
        {
            return this.contractTemplate != null;
        }

        private async Task AddPart()
        {
            this.saveState = true;
            string code = await QrCode.ScanQrCode(this.navigationService, AppResources.Add);
            this.saveState = false;
            this.DeleteState();
            string id = Constants.IoTSchemes.GetCode(code);
            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(id))
            {
                AddRole(new RoleModel(id));
            }
        }

        private void AddRole(RoleModel model)
        {
            if (model != null)
            {
                this.ContractRoles.Add(model);
            }
        }

        private void RemoveRole(RoleModel model)
        {
            if (model != null)
            {
                RoleModel existing = this.ContractRoles.FirstOrDefault(x => x.Name == model.Name);
                if (existing != null)
                {
                    this.ContractRoles.Remove(existing);
                }
            }
        }

        private void EditParameter(object obj)
        {
            if (!(obj is ParameterModel model))
                return;

            Parameter p = this.contractTemplate.Parameters.FirstOrDefault(x => x.Name == model.Id);
            if (p != null)
            {
                if (p is StringParameter sp)
                {
                    sp.Value = model.Name;
                    model.IsValid = true;
                }
                else if (p is NumericalParameter np)
                {
                    if (double.TryParse(model.Name, out double d))
                    {
                        np.Value = d;
                        model.IsValid = true;
                    }
                    else
                    {
                        model.IsValid = false;
                    }
                }
                LoadHumanReadableText();
            }
        }

        private async Task ShowLegalId(string legalId)
        {
            try
            {
                await this.contractOrchestratorService.OpenLegalIdentity(legalId, "For inclusion as part in a contract.");
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }
    }
}