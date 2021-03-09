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
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.ViewModels.Contracts
{
    /// <summary>
    /// The view model to bind to when displaying a new contract view or page.
    /// </summary>
    public class NewContractViewModel : BaseViewModel
    {
        private Contract template;
        private readonly ILogService logService;
        private readonly INeuronService neuronService;
        private readonly INavigationService navigationService;
        private readonly IUiDispatcher uiDispatcher;
        private readonly ISettingsService settingsService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly ITagProfile tagProfile;
        private string templateId;
        private bool saveState;
        private Contract stateTemplate;

        /// <summary>
        /// Creates an instance of the <see cref="NewContractViewModel"/> class.
        /// </summary>
        public NewContractViewModel()
            : this(null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="NewContractViewModel"/> class.
        /// For unit tests.
        /// <param name="tagProfile">The tag profile to work with.</param>
        /// <param name="logService">The log service.</param>
        /// <param name="neuronService">The Neuron service for XMPP communication.</param>
        /// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
        /// <param name="navigationService">The navigation service to use for app navigation</param>
        /// <param name="settingsService">The settings service for persisting UI state.</param>
        /// <param name="contractOrchestratorService">The service to use for contract orchestration.</param>
        /// </summary>
        protected internal NewContractViewModel(
            ITagProfile tagProfile,
            ILogService logService,
            INeuronService neuronService,
            IUiDispatcher uiDispatcher,
            INavigationService navigationService,
            ISettingsService settingsService,
            IContractOrchestratorService contractOrchestratorService)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<ITagProfile>();
            this.logService = logService ?? DependencyService.Resolve<ILogService>();
            this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            this.uiDispatcher = uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            this.settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            this.contractOrchestratorService = contractOrchestratorService ?? DependencyService.Resolve<IContractOrchestratorService>();

            this.ContractVisibilityItems = new ObservableCollection<ContractVisibilityModel>();
            this.AvailableRoles = new ObservableCollection<string>();
            this.ProposeCommand = new Command(async _ => await this.Propose(), _ => this.CanPropose());
            this.Roles = new StackLayout();
            this.Parameters = new StackLayout();
            this.HumanReadableText = new StackLayout();
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();

            if (this.navigationService.TryPopArgs(out NewContractNavigationArgs args))
            {
                this.template = args.Contract;
            }
            else if (!(this.stateTemplate is null))
            {
                this.template = this.stateTemplate;
                this.stateTemplate = null;
            }
            this.templateId = this.template?.ContractId ?? string.Empty;
            this.IsTemplate = this.template?.CanActAsTemplate ?? false;

            this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.CreatorAndParts, AppResources.ContractVisibility_CreatorAndParts));
            this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.DomainAndParts, AppResources.ContractVisibility_DomainAndParts));
            this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.Public, AppResources.ContractVisibility_Public));
            this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.PublicSearchable, AppResources.ContractVisibility_PublicSearchable));

            this.PopulateTemplateForm();
            this.uiDispatcher.BeginInvokeOnMainThread(() =>
            {
                this.Roles.ForceLayout();
                this.Parameters.ForceLayout();
                this.HumanReadableText.ForceLayout();
            });
        }

        /// <inheritdoc/>
        protected override async Task DoUnbind()
        {
            this.ContractVisibilityItems.Clear();

            this.ClearTemplate(false);

            await base.DoUnbind();
        }

        /// <inheritdoc/>
        protected override async Task DoSaveState()
        {
            await base.DoSaveState();

            if (!(this.SelectedContractVisibilityItem is null))
            {
                ContractVisibility value = this.SelectedContractVisibilityItem.Visibility;
                await this.settingsService.SaveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)), value.ToString());
            }
            if (!(SelectedRole is null))
            {
                await this.settingsService.SaveState(GetSettingsKey(nameof(SelectedRole)), this.SelectedRole);
            }
        }

        /// <inheritdoc/>
        protected override async Task DoRestoreState()
        {
            if (this.saveState)
            {
                string visibilityStr = await this.settingsService.RestoreStringState(nameof(SelectedContractVisibilityItem));
                if (Enum.TryParse(visibilityStr, out ContractVisibility cv))
                {
                    this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == cv);
                }

                string selectedRole = await this.settingsService.RestoreStringState(GetSettingsKey(nameof(SelectedRole)));
                string matchingRole = this.AvailableRoles.FirstOrDefault(x => x.Equals(selectedRole));
                if (!string.IsNullOrWhiteSpace(matchingRole))
                {
                    this.SelectedRole = matchingRole;
                }
            }
            await base.DoRestoreState();
        }

        private void DeleteState()
        {
            this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
            this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedRole)));
        }

        #region Properties

        /// <summary>
        /// The Propose action which creates a new contract.
        /// </summary>
        public ICommand ProposeCommand { get; }

        /// <summary>
        /// See <see cref="IsTemplate"/>
        /// </summary>
        public static readonly BindableProperty IsTemplateProperty =
            BindableProperty.Create("IsTemplate", typeof(bool), typeof(NewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether this contract is a template or not.
        /// </summary>
        public bool IsTemplate
        {
            get { return (bool)GetValue(IsTemplateProperty); }
            set { SetValue(IsTemplateProperty, value); }
        }

        /// <summary>
        /// A list of valid visibility items to choose from for this contract.
        /// </summary>
        public ObservableCollection<ContractVisibilityModel> ContractVisibilityItems { get; }

        /// <summary>
        /// See <see cref="SelectedContractVisibilityItem"/>
        /// </summary>
        public static readonly BindableProperty SelectedContractVisibilityItemProperty =
            BindableProperty.Create("SelectedContractVisibilityItem", typeof(ContractVisibilityModel), typeof(NewContractViewModel), default(ContractVisibilityModel));

        /// <summary>
        /// The selected contract visibility item, if any.
        /// </summary>
        public ContractVisibilityModel SelectedContractVisibilityItem
        {
            get { return (ContractVisibilityModel)GetValue(SelectedContractVisibilityItemProperty); }
            set { SetValue(SelectedContractVisibilityItemProperty, value); }
        }

        /// <summary>
        /// See <see cref="VisibilityIsEnabled"/>
        /// </summary>
        public static readonly BindableProperty VisibilityIsEnabledProperty =
            BindableProperty.Create("VisibilityIsEnabled", typeof(bool), typeof(NewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the visibility items should be shown to the user or not.
        /// </summary>
        public bool VisibilityIsEnabled
        {
            get { return (bool)GetValue(VisibilityIsEnabledProperty); }
            set { SetValue(VisibilityIsEnabledProperty, value); }
        }

        /// <summary>
        /// See <see cref="Pin"/>
        /// </summary>
        public static readonly BindableProperty PinProperty =
            BindableProperty.Create("Pin", typeof(string), typeof(NewContractViewModel), default(string));

        /// <summary>
        /// Gets or sets the user selected Pin. Can be null.
        /// </summary>
        public string Pin
        {
            get { return (string)GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        /// <summary>
        /// The different roles available to choose from when creating a contract.
        /// </summary>
        public ObservableCollection<string> AvailableRoles { get; }

        /// <summary>
        /// See <see cref="SelectedRole"/>
        /// </summary>
        public static readonly BindableProperty SelectedRoleProperty =
            BindableProperty.Create("SelectedRole", typeof(string), typeof(NewContractViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                NewContractViewModel viewModel = (NewContractViewModel)b;
                string oldRole = (string)oldValue;
                viewModel.RemoveRole(oldRole, viewModel.tagProfile.LegalIdentity.Id);
                string newRole = (string)newValue;
                if (!(viewModel.template is null) && !string.IsNullOrWhiteSpace(newRole))
                {
                    viewModel.AddRole(newRole, viewModel.tagProfile.LegalIdentity.Id);
                }
            });

        /// <summary>
        /// The role selected for the contract, if any.
        /// </summary>
        public string SelectedRole
        {
            get { return (string)GetValue(SelectedRoleProperty); }
            set { SetValue(SelectedRoleProperty, value); }
        }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's roles.
        /// </summary>
        public StackLayout Roles { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's parameters.
        /// </summary>
        public StackLayout Parameters { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's human readable text section.
        /// </summary>
        public StackLayout HumanReadableText { get; }

        /// <summary>
        /// See <see cref="UsePin"/>
        /// </summary>
        public static readonly BindableProperty UsePinProperty =
            BindableProperty.Create("UsePin", typeof(bool), typeof(NewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether a pin should be used for added security.
        /// </summary>
        public bool UsePin
        {
            get { return (bool)GetValue(UsePinProperty); }
            set { SetValue(UsePinProperty, value); }
        }

        /// <summary>
        /// See <see cref="HasRoles"/>
        /// </summary>
        public static readonly BindableProperty HasRolesProperty =
            BindableProperty.Create("HasRoles", typeof(bool), typeof(NewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the contract has roles.
        /// </summary>
        public bool HasRoles
        {
            get { return (bool)GetValue(HasRolesProperty); }
            set { SetValue(HasRolesProperty, value); }
        }

        /// <summary>
        /// See <see cref="HasParameters"/>
        /// </summary>
        public static readonly BindableProperty HasParametersProperty =
            BindableProperty.Create("HasParameters", typeof(bool), typeof(NewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the contract has parameters.
        /// </summary>
        public bool HasParameters
        {
            get { return (bool)GetValue(HasParametersProperty); }
            set { SetValue(HasParametersProperty, value); }
        }

        /// <summary>
        /// See <see cref="HasHumanReadableText"/>
        /// </summary>
        public static readonly BindableProperty HasHumanReadableTextProperty =
            BindableProperty.Create("HasHumanReadableText", typeof(bool), typeof(NewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether the contract is comprised of human readable text.
        /// </summary>
        public bool HasHumanReadableText
        {
            get { return (bool)GetValue(HasHumanReadableTextProperty); }
            set { SetValue(HasHumanReadableTextProperty, value); }
        }

        /// <summary>
        /// See <see cref="CanAddParts"/>
        /// </summary>
        public static readonly BindableProperty CanAddPartsProperty =
            BindableProperty.Create("CanAddParts", typeof(bool), typeof(NewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether a user can add parts to a contract.
        /// </summary>
        public bool CanAddParts
        {
            get { return (bool)GetValue(CanAddPartsProperty); }
            set { SetValue(CanAddPartsProperty, value); }
        }

        #endregion

        private void ClearTemplate(bool propertiesOnly)
        {
            if (!propertiesOnly)
            {
                this.template = null;
            }

            this.SelectedRole = null;
            this.AvailableRoles.Clear();

            this.Roles.Children.Clear();
            this.HasRoles = false;

            this.Parameters.Children.Clear();
            this.HasParameters = false;

            this.HumanReadableText.Children.Clear();
            this.HasHumanReadableText = false;

            this.UsePin = false;
            this.CanAddParts = false;
            this.VisibilityIsEnabled = false;
            this.EvaluateCommands(this.ProposeCommand);
        }

        private void RemoveRole(string role, string legalId)
        {
            Label ToRemove = null;
            int State = 0;

            foreach (View View in this.Roles.Children)
            {
                switch (State)
                {
                    case 0:
                        if (View is Label Label && Label.StyleId == role)
                            State++;
                        break;

                    case 1:
                        if (View is Button Button)
                        {
                            if (!(ToRemove is null))
                            {
                                this.Roles.Children.Remove(ToRemove);
                                Button.IsEnabled = true;
                            }
                            return;
                        }
                        else if (View is Label Label2 && Label2.StyleId == legalId)
                            ToRemove = Label2;
                        break;
                }
            }
        }

        private void AddRole(string role, string legalId)
        {
            int State = 0;
            int i = 0;
            int NrParts = 0;
            Role RoleObj = null;

            foreach (Role R in this.template.Roles)
            {
                if (R.Name == role)
                {
                    RoleObj = R;
                    break;
                }
            }

            if (RoleObj is null)
                return;

            foreach (View View in this.Roles.Children)
            {
                switch (State)
                {
                    case 0:
                        if (View is Label Label && Label.StyleId == role)
                            State++;
                        break;

                    case 1:
                        if (View is Button Button)
                        {
                            TapGestureRecognizer OpenLegalId = new TapGestureRecognizer();
                            OpenLegalId.Tapped += this.LegalId_Tapped;

                            Label = new Label
                            {
                                Text = legalId,
                                StyleId = legalId,
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                                HorizontalTextAlignment = TextAlignment.Center,
                                FontAttributes = FontAttributes.Bold
                            };

                            Label.GestureRecognizers.Add(OpenLegalId);

                            this.Roles.Children.Insert(i, Label);
                            NrParts++;

                            if (NrParts >= RoleObj.MaxCount)
                                Button.IsEnabled = false;

                            return;
                        }
                        else if (View is Label)
                            NrParts++;
                        break;
                }

                i++;
            }
        }

        private async void LegalId_Tapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Label label && !string.IsNullOrEmpty(label.StyleId))
                {
                    await this.contractOrchestratorService.OpenLegalIdentity(label.StyleId, "For inclusion as part in a contract.");
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async void AddPartButton_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                this.saveState = true;
                this.stateTemplate = this.template;
                string code = await QrCode.ScanQrCode(this.navigationService, AppResources.Add);
                this.saveState = false;
                this.DeleteState();
                string id = Constants.UriSchemes.GetCode(code);
                if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(id))
                {
                    AddRole(button.StyleId, id);
                }
            }
        }

        private void Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry Entry)
            {
                foreach (Parameter P in this.template.Parameters)
                {
                    if (P.Name == Entry.StyleId)
                    {
                        if (P is StringParameter SP)
                            SP.Value = e.NewTextValue;
                        else if (P is NumericalParameter NP)
                        {
                            if (double.TryParse(e.NewTextValue, out double d))
                            {
                                NP.Value = d;
                                Entry.BackgroundColor = Color.Default;
                            }
                            else
                            {
                                Entry.BackgroundColor = Color.Salmon;
                                return;
                            }
                        }

                        PopulateHumanReadableText();
                        break;
                    }
                }
            }
        }

        private async Task Propose()
        {
            List<Part> Parts = new List<Part>();
            Contract Created = null;
            string Role = string.Empty;
            int State = 0;
            int Nr = 0;
            int Min = 0;
            int Max = 0;

            try
            {
                foreach (View View in this.Roles.Children)
                {
                    switch (State)
                    {
                        case 0:
                            if (View is Label Label && !string.IsNullOrEmpty(Label.StyleId))
                            {
                                Role = Label.StyleId;
                                State++;
                                Nr = Min = Max = 0;

                                foreach (Role R in this.template.Roles)
                                {
                                    if (R.Name == Role)
                                    {
                                        Min = R.MinCount;
                                        Max = R.MaxCount;
                                        break;
                                    }
                                }
                            }
                            break;

                        case 1:
                            if (View is Button)
                            {
                                if (Nr < Min)
                                {
                                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.TheContractRequiresAtLeast_AddMoreParts, Min, Role));
                                    return;
                                }

                                if (Nr > Min)
                                {
                                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.TheContractRequiresAtMost_RemoveParts, Max, Role));
                                    return;
                                }

                                State--;
                                Role = string.Empty;
                            }
                            else if (View is Label Label2 && !string.IsNullOrEmpty(Role))
                            {
                                Parts.Add(new Part
                                {
                                    Role = Role,
                                    LegalId = Label2.StyleId
                                });

                                Nr++;
                            }
                            break;
                    }
                }

                foreach (View View in this.Parameters.Children)
                {
                    if (View is Entry Entry)
                    {
                        if (Entry.BackgroundColor == Color.Salmon)
                        {
                            await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.YourContractContainsErrors);
                            Entry.Focus();
                            return;
                        }
                    }
                }

                this.template.PartsMode = ContractParts.Open;
                int i = this.ContractVisibilityItems.IndexOf(this.SelectedContractVisibilityItem);
                switch (i)
                {
                    case 0:
                        this.template.Visibility = ContractVisibility.CreatorAndParts;
                        break;

                    case 1:
                        this.template.Visibility = ContractVisibility.DomainAndParts;
                        break;

                    case 2:
                        this.template.Visibility = ContractVisibility.Public;
                        break;

                    case 3:
                        this.template.Visibility = ContractVisibility.PublicSearchable;
                        break;

                    default:
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.ContractVisibilityMustBeSelected);
                        return;
                }

                if ((this.SelectedRole is null))
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.ContractRoleMustBeSelected);
                    return;
                }

                if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
                {
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
                    return;
                }

                Created = await this.neuronService.Contracts.CreateContract(this.templateId, Parts.ToArray(), this.template.Parameters,
                    this.template.Visibility, ContractParts.ExplicitlyDefined, this.template.Duration, this.template.ArchiveRequired,
                    this.template.ArchiveOptional, null, null, false);

                Created = await this.neuronService.Contracts.SignContract(Created, this.SelectedRole, false);
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
            finally
            {
                if (!(Created is null))
                {
                    await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(Created, false));
                }
            }
        }

        internal static string FilterDefaultValues(string s)
        {
            foreach (char ch in s)
            {
                if (char.ToUpper(ch) != ch)
                    return s;
            }

            return string.Empty;
        }

        internal static void Populate(StackLayout Layout, string Xaml)
        {
            StackLayout xaml = new StackLayout().LoadFromXaml(Xaml);

            List<View> children = new List<View>();
            children.AddRange(xaml.Children);

            foreach (View Element in children)
                Layout.Children.Add(Element);
        }

        private void PopulateTemplateForm()
        {
            this.ClearTemplate(true);

            if (this.template is null)
            {
                return;
            }

            this.PopulateHumanReadableText();

            this.HasRoles = this.template.Roles.Length > 0;
            this.VisibilityIsEnabled = true;

            foreach (Role Role in this.template.Roles)
            {
                this.AvailableRoles.Add(Role.Name);

                this.Roles.Children.Add(new Label
                {
                    Text = Role.Name,
                    Style = (Style)Application.Current.Resources["LeftAlignedHeading"],
                    StyleId = Role.Name
                });

                Populate(this.Roles, Role.ToXamarinForms(this.template.DefaultLanguage, this.template));

                if (Role.MinCount > 0)
                {
                    Button button = new Button
                    {
                        Text = AppResources.AddPart,
                        StyleId = Role.Name,
                        Margin = (Thickness)Application.Current.Resources["DefaultBottomOnlyMargin"]
                    };
                    button.Clicked += AddPartButton_Clicked;

                    this.Roles.Children.Add(button);
                }
            }

            if (template.Parameters.Length > 0)
            {
                this.Parameters.Children.Add(new Label
                {
                    Text = AppResources.Parameters,
                    Style = (Style)Application.Current.Resources["LeftAlignedHeading"]
                });
            }

            foreach (Parameter Parameter in this.template.Parameters)
            {
                Populate(Parameters, Parameter.ToXamarinForms(this.template.DefaultLanguage, this.template));

                Entry Entry;
                Parameters.Children.Add(Entry = new Entry
                {
                    StyleId = Parameter.Name,
                    Text = FilterDefaultValues(Parameter.ObjectValue?.ToString()),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                });

                Entry.TextChanged += Entry_TextChanged;
            }

            this.HasParameters = this.Parameters.Children.Count > 0;

            this.UsePin = this.tagProfile.UsePin;

            this.EvaluateCommands(this.ProposeCommand);
        }

        private void PopulateHumanReadableText()
        {
            this.HumanReadableText.Children.Clear();

            if (!(this.template is null))
                Populate(this.HumanReadableText, this.template.ToXamarinForms(this.template.DefaultLanguage));

            this.HasHumanReadableText = this.HumanReadableText.Children.Count > 0;
        }

        private bool CanPropose()
        {
            return !(this.template is null);
        }
    }
}