using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Extensions;
using IdApp.Pages.Contracts.NewContract.ObjectModel;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Pages.Main.Main;
using IdApp.Services.Contracts;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Settings;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using IdApp.Services.UI.QR;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.NewContract
{
	/// <summary>
	/// The view model to bind to when displaying a new contract view or page.
	/// </summary>
	public class NewContractViewModel : BaseViewModel
	{
		private static readonly string PartSettingsPrefix = $"{typeof(NewContractViewModel)}.Part_";
		private Contract template;
		private readonly ILogService logService;
		private readonly INeuronService neuronService;
		private readonly INavigationService navigationService;
		private readonly IUiSerializer uiSerializer;
		private readonly ISettingsService settingsService;
		private readonly IContractOrchestratorService contractOrchestratorService;
		private readonly ITagProfile tagProfile;
		private string templateId;
		private bool saveStateWhileScanning;
		private Contract stateTemplateWhileScanning;
		private readonly Dictionary<string, string> partsToAdd;

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
		/// <param name="uiSerializer">The UI dispatcher for alerts.</param>
		/// <param name="navigationService">The navigation service to use for app navigation</param>
		/// <param name="settingsService">The settings service for persisting UI state.</param>
		/// <param name="contractOrchestratorService">The service to use for contract orchestration.</param>
		/// </summary>
		protected internal NewContractViewModel(
			ITagProfile tagProfile,
			ILogService logService,
			INeuronService neuronService,
			IUiSerializer uiSerializer,
			INavigationService navigationService,
			ISettingsService settingsService,
			IContractOrchestratorService contractOrchestratorService)
		{
			this.tagProfile = tagProfile ?? App.Instantiate<ITagProfile>();
			this.logService = logService ?? App.Instantiate<ILogService>();
			this.neuronService = neuronService ?? App.Instantiate<INeuronService>();
			this.uiSerializer = uiSerializer ?? App.Instantiate<IUiSerializer>();
			this.navigationService = navigationService ?? App.Instantiate<INavigationService>();
			this.settingsService = settingsService ?? App.Instantiate<ISettingsService>();
			this.contractOrchestratorService = contractOrchestratorService ?? App.Instantiate<IContractOrchestratorService>();

			this.ContractVisibilityItems = new ObservableCollection<ContractVisibilityModel>();
			this.AvailableRoles = new ObservableCollection<string>();
			this.ProposeCommand = new Command(async _ => await this.Propose(), _ => this.CanPropose());
			this.partsToAdd = new Dictionary<string, string>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out NewContractNavigationArgs args))
			{
				this.template = args.Contract;
			}
			else if (!(this.stateTemplateWhileScanning is null))
			{
				this.template = this.stateTemplateWhileScanning;
				this.stateTemplateWhileScanning = null;
			}
			this.templateId = this.template?.ContractId ?? string.Empty;
			this.IsTemplate = this.template?.CanActAsTemplate ?? false;

			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.CreatorAndParts, AppResources.ContractVisibility_CreatorAndParts));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.DomainAndParts, AppResources.ContractVisibility_DomainAndParts));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.Public, AppResources.ContractVisibility_Public));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.PublicSearchable, AppResources.ContractVisibility_PublicSearchable));

			this.PopulateTemplateForm();
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.ContractVisibilityItems.Clear();

			this.ClearTemplate(false);

			if (!this.saveStateWhileScanning)
			{
				await this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
				await this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedRole)));
				await this.settingsService.RemoveStateWhereKeyStartsWith(PartSettingsPrefix);
			}
			await base.DoUnbind();
		}

		/// <inheritdoc/>
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();

			if (!(this.SelectedContractVisibilityItem is null))
				await this.settingsService.SaveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)), this.SelectedContractVisibilityItem.Visibility);
			else
				await this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
			
			if (!(SelectedRole is null))
				await this.settingsService.SaveState(GetSettingsKey(nameof(SelectedRole)), this.SelectedRole);
			else
				await this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedRole)));
			
			if (this.partsToAdd.Count > 0)
			{
				foreach (KeyValuePair<string, string> part in this.partsToAdd)
				{
					string settingsKey = $"{PartSettingsPrefix}{part.Key}";
					await this.settingsService.SaveState(settingsKey, part.Value);
				}
			}
			else
				await this.settingsService.RemoveStateWhereKeyStartsWith(PartSettingsPrefix);
			
			this.partsToAdd.Clear();
		}

		/// <inheritdoc/>
		protected override async Task DoRestoreState()
		{
			if (this.saveStateWhileScanning)
			{
				Enum e = await this.settingsService.RestoreEnumState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
				if (e != null)
				{
					ContractVisibility cv = (ContractVisibility)e;
					this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == cv);
				}

				string selectedRole = await this.settingsService.RestoreStringState(GetSettingsKey(nameof(SelectedRole)));
				string matchingRole = this.AvailableRoles.FirstOrDefault(x => x.Equals(selectedRole));
				
				if (!string.IsNullOrWhiteSpace(matchingRole))
					this.SelectedRole = matchingRole;

				List<(string key, string value)> settings = (await this.settingsService.RestoreStateWhereKeyStartsWith<string>(PartSettingsPrefix)).ToList();
				if (settings.Count > 0)
				{
					this.partsToAdd.Clear();
					foreach ((string key, string value) in settings)
					{
						string part = key.Substring(PartSettingsPrefix.Length);
						this.partsToAdd[part] = value;
					}
				}

				if (this.partsToAdd.Count > 0)
				{
					foreach (KeyValuePair<string, string> part in this.partsToAdd)
						this.AddRole(part.Key, part.Value);
				}

				await this.DeleteState();
			}

			this.saveStateWhileScanning = false;
			await base.DoRestoreState();
		}

		private async Task DeleteState()
		{
			await this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
			await this.settingsService.RemoveState(GetSettingsKey(nameof(SelectedRole)));
			await this.settingsService.RemoveStateWhereKeyStartsWith(PartSettingsPrefix);
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
		/// See <see cref="Roles"/>
		/// </summary>
		public static readonly BindableProperty RolesProperty =
			BindableProperty.Create("Roles", typeof(StackLayout), typeof(NewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's roles.
		/// </summary>
		public StackLayout Roles
		{
			get { return (StackLayout)GetValue(RolesProperty); }
			set { SetValue(RolesProperty, value); }
		}

		/// <summary>
		/// See <see cref="Parameters"/>
		/// </summary>
		public static readonly BindableProperty ParametersProperty =
			BindableProperty.Create("Parameters", typeof(StackLayout), typeof(NewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parameters.
		/// </summary>
		public StackLayout Parameters
		{
			get { return (StackLayout)GetValue(ParametersProperty); }
			set { SetValue(ParametersProperty, value); }
		}

		/// <summary>
		/// See <see cref="HumanReadableText"/>
		/// </summary>
		public static readonly BindableProperty HumanReadableTextProperty =
			BindableProperty.Create("HumanReadableText", typeof(StackLayout), typeof(NewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's human readable text section.
		/// </summary>
		public StackLayout HumanReadableText
		{
			get { return (StackLayout)GetValue(HumanReadableTextProperty); }
			set { SetValue(HumanReadableTextProperty, value); }
		}

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
				this.template = null;

			this.SelectedRole = null;
			this.AvailableRoles.Clear();

			this.Roles = null;
			this.HasRoles = false;

			this.Parameters = null;
			this.HasParameters = false;

			this.HumanReadableText = null;
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

			if ((this.Roles is null))
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

			Contract contractToUse = this.template ?? this.stateTemplateWhileScanning;

			if ((contractToUse is null) || (this.Roles is null))
				return;

			foreach (Role R in contractToUse.Roles)
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
					await this.contractOrchestratorService.OpenLegalIdentity(label.StyleId, "For inclusion as part in a contract.");
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiSerializer.DisplayAlert(ex);
			}
		}

		private async void AddPartButton_Clicked(object sender, EventArgs e)
		{
			if (sender is Button button)
			{
				this.saveStateWhileScanning = true;
				this.stateTemplateWhileScanning = this.template;
				await QrCode.ScanQrCode(this.navigationService, AppResources.Add, async code =>
				{
					string id = Constants.UriSchemes.RemoveScheme(code);
					if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(id))
					{
						this.partsToAdd[button.StyleId] = id;
						string settingsKey = $"{PartSettingsPrefix}{button.StyleId}";
						await this.settingsService.SaveState(settingsKey, id);
					}
				});
			}
		}

		private void Parameter_TextChanged(object sender, TextChangedEventArgs e)
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
						else if (P is BooleanParameter BP)
						{
							if (bool.TryParse(e.NewTextValue, out bool b))
							{
								BP.Value = b;
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

		private void Parameter_CheckedChanged(object sender, CheckedChangedEventArgs e)
		{
			if (sender is CheckBox CheckBox)
			{
				foreach (Parameter P in this.template.Parameters)
				{
					if (P.Name == CheckBox.StyleId)
					{
						if (P is BooleanParameter BP)
							BP.Value = e.Value;

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
									await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.TheContractRequiresAtLeast_AddMoreParts, Min, Role));
									return;
								}

								if (Nr > Min)
								{
									await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.TheContractRequiresAtMost_RemoveParts, Max, Role));
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
							await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.YourContractContainsErrors);
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
						await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.ContractVisibilityMustBeSelected);
						return;
				}

				if ((this.SelectedRole is null))
				{
					await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.ContractRoleMustBeSelected);
					return;
				}

				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.Pin) != this.tagProfile.PinHash)
				{
					await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.PinIsInvalid);
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
				await this.uiSerializer.DisplayAlert(ex);
			}
			finally
			{
				if (!(Created is null))
				{
					await this.navigationService.GoToAsync(nameof(Pages.Contracts.ViewContract.ViewContractPage), new ViewContractNavigationArgs(Created, false)
					{
						ReturnRoute = $"///{nameof(MainPage)}"
					});
				}
			}
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
				return;

			this.PopulateHumanReadableText();

			this.HasRoles = this.template.Roles.Length > 0;
			this.VisibilityIsEnabled = true;

			StackLayout rolesLayout = new StackLayout();
			foreach (Role Role in this.template.Roles)
			{
				this.AvailableRoles.Add(Role.Name);

				rolesLayout.Children.Add(new Label
				{
					Text = Role.Name,
					Style = (Style)Application.Current.Resources["LeftAlignedHeading"],
					StyleId = Role.Name
				});

				Populate(rolesLayout, Role.ToXamarinForms(this.template.DeviceLanguage(), this.template));

				if (Role.MinCount > 0)
				{
					Button button = new Button
					{
						Text = AppResources.AddPart,
						StyleId = Role.Name,
						Margin = (Thickness)Application.Current.Resources["DefaultBottomOnlyMargin"]
					};
					button.Clicked += AddPartButton_Clicked;

					rolesLayout.Children.Add(button);
				}
			}
			this.Roles = rolesLayout;

			StackLayout parametersLayout = new StackLayout();
			if (template.Parameters.Length > 0)
			{
				parametersLayout.Children.Add(new Label
				{
					Text = AppResources.Parameters,
					Style = (Style)Application.Current.Resources["LeftAlignedHeading"]
				});
			}

			foreach (Parameter Parameter in this.template.Parameters)
			{
				if (Parameter is BooleanParameter BP)
				{
					CheckBox CheckBox = new CheckBox()
					{
						StyleId = Parameter.Name,
						IsChecked = BP.Value,
						VerticalOptions = LayoutOptions.Center
					};

					StackLayout Layout = new StackLayout()
					{
						Orientation = StackOrientation.Horizontal
					};

					Layout.Children.Add(CheckBox);
					Populate(Layout, Parameter.ToXamarinForms(this.template.DeviceLanguage(), this.template));
					parametersLayout.Children.Add(Layout);

					CheckBox.CheckedChanged += Parameter_CheckedChanged;
				}
				else
				{
					Populate(parametersLayout, Parameter.ToXamarinForms(this.template.DeviceLanguage(), this.template));

					Entry Entry = new Entry()
					{
						StyleId = Parameter.Name,
						Text = Parameter.ObjectValue?.ToString(), 
						Placeholder = Parameter.Guide,
						HorizontalOptions = LayoutOptions.FillAndExpand,
					};

					parametersLayout.Children.Add(Entry);

					Entry.TextChanged += Parameter_TextChanged;
				}
			}

			this.Parameters = parametersLayout;
			this.HasParameters = this.Parameters.Children.Count > 0;
			this.UsePin = this.tagProfile.UsePin;
			
			this.EvaluateCommands(this.ProposeCommand);
		}

		private void PopulateHumanReadableText()
		{
			this.HumanReadableText = null;

			StackLayout humanReadableTextLayout = new StackLayout();

			if (!(this.template is null))
				Populate(humanReadableTextLayout, this.template.ToXamarinForms(this.template.DeviceLanguage()));

			this.HumanReadableText = humanReadableTextLayout;
			this.HasHumanReadableText = humanReadableTextLayout.Children.Count > 0;
		}

		private bool CanPropose()
		{
			return !(this.template is null);
		}
	}
}