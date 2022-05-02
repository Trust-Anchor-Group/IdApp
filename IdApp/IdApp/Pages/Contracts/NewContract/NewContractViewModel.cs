using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Controls.Extended;
using IdApp.Extensions;
using IdApp.Pages.Contacts;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts.NewContract.ObjectModel;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Pages.Main.Main;
using IdApp.Resx;
using IdApp.Services;
using Waher.Content;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Script;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.NewContract
{
	/// <summary>
	/// The view model to bind to when displaying a new contract view or page.
	/// </summary>
	public class NewContractViewModel : BaseViewModel
	{
		private static readonly string PartSettingsPrefix = typeof(NewContractViewModel).FullName + ".Part_";

		private readonly Dictionary<string, ParameterInfo> parametersByName = new();
		private Contract template;
		private string templateId;
		private bool saveStateWhileScanning;
		private Contract stateTemplateWhileScanning;
		private readonly Dictionary<string, string> partsToAdd;

		/// <summary>
		/// Creates an instance of the <see cref="NewContractViewModel"/> class.
		/// </summary>
		protected internal NewContractViewModel()
		{
			this.ContractVisibilityItems = new ObservableCollection<ContractVisibilityModel>();
			this.AvailableRoles = new ObservableCollection<string>();
			this.ProposeCommand = new Command(async _ => await this.Propose(), _ => this.CanPropose());
			this.partsToAdd = new Dictionary<string, string>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			bool FirstTime = false;
			ContractVisibility? Visibility = null;

			if (this.NavigationService.TryPopArgs(out NewContractNavigationArgs args))
			{
				this.template = args.Template;

				if (args.SetVisibility)
					Visibility = args.Template.Visibility;

				FirstTime = true;
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

			await this.PopulateTemplateForm(FirstTime, Visibility);
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.ContractVisibilityItems.Clear();

			this.ClearTemplate(false);

			if (!this.saveStateWhileScanning)
			{
				await this.SettingsService.RemoveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
				await this.SettingsService.RemoveState(GetSettingsKey(nameof(SelectedRole)));
				await this.SettingsService.RemoveStateWhereKeyStartsWith(PartSettingsPrefix);
			}
			await base.DoUnbind();
		}

		/// <inheritdoc/>
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();

			if (!(this.SelectedContractVisibilityItem is null))
				await this.SettingsService.SaveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)), this.SelectedContractVisibilityItem.Visibility);
			else
				await this.SettingsService.RemoveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));

			if (!(SelectedRole is null))
				await this.SettingsService.SaveState(GetSettingsKey(nameof(SelectedRole)), this.SelectedRole);
			else
				await this.SettingsService.RemoveState(GetSettingsKey(nameof(SelectedRole)));

			if (this.partsToAdd.Count > 0)
			{
				foreach (KeyValuePair<string, string> part in this.partsToAdd)
				{
					string settingsKey = PartSettingsPrefix + part.Key;
					await this.SettingsService.SaveState(settingsKey, part.Value);
				}
			}
			else
				await this.SettingsService.RemoveStateWhereKeyStartsWith(PartSettingsPrefix);

			this.partsToAdd.Clear();
		}

		/// <inheritdoc/>
		protected override async Task DoRestoreState()
		{
			if (this.saveStateWhileScanning)
			{
				Enum e = await this.SettingsService.RestoreEnumState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
				if (!(e is null))
				{
					ContractVisibility cv = (ContractVisibility)e;
					this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == cv);
				}

				string selectedRole = await this.SettingsService.RestoreStringState(GetSettingsKey(nameof(SelectedRole)));
				string matchingRole = this.AvailableRoles.FirstOrDefault(x => x.Equals(selectedRole));

				if (!string.IsNullOrWhiteSpace(matchingRole))
					this.SelectedRole = matchingRole;

				List<(string key, string value)> settings = (await this.SettingsService.RestoreStateWhereKeyStartsWith<string>(PartSettingsPrefix)).ToList();
				if (settings.Count > 0)
				{
					this.partsToAdd.Clear();
					foreach ((string key, string value) in settings)
					{
						string part = key[PartSettingsPrefix.Length..];
						this.partsToAdd[part] = value;
					}
				}

				if (this.partsToAdd.Count > 0)
				{
					foreach (KeyValuePair<string, string> part in this.partsToAdd)
						await this.AddRole(part.Key, part.Value);
				}

				await this.DeleteState();
			}

			this.saveStateWhileScanning = false;
			await base.DoRestoreState();
		}

		private async Task DeleteState()
		{
			await this.SettingsService.RemoveState(GetSettingsKey(nameof(SelectedContractVisibilityItem)));
			await this.SettingsService.RemoveState(GetSettingsKey(nameof(SelectedRole)));
			await this.SettingsService.RemoveStateWhereKeyStartsWith(PartSettingsPrefix);
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
			BindableProperty.Create(nameof(IsTemplate), typeof(bool), typeof(NewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether this contract is a template or not.
		/// </summary>
		public bool IsTemplate
		{
			get => (bool)this.GetValue(IsTemplateProperty);
			set => this.SetValue(IsTemplateProperty, value);
		}

		/// <summary>
		/// A list of valid visibility items to choose from for this contract.
		/// </summary>
		public ObservableCollection<ContractVisibilityModel> ContractVisibilityItems { get; }

		/// <summary>
		/// See <see cref="SelectedContractVisibilityItem"/>
		/// </summary>
		public static readonly BindableProperty SelectedContractVisibilityItemProperty =
			BindableProperty.Create(nameof(SelectedContractVisibilityItem), typeof(ContractVisibilityModel), typeof(NewContractViewModel), default(ContractVisibilityModel));

		/// <summary>
		/// The selected contract visibility item, if any.
		/// </summary>
		public ContractVisibilityModel SelectedContractVisibilityItem
		{
			get => (ContractVisibilityModel)this.GetValue(SelectedContractVisibilityItemProperty);
			set => this.SetValue(SelectedContractVisibilityItemProperty, value);
		}

		/// <summary>
		/// See <see cref="VisibilityIsEnabled"/>
		/// </summary>
		public static readonly BindableProperty VisibilityIsEnabledProperty =
			BindableProperty.Create(nameof(VisibilityIsEnabled), typeof(bool), typeof(NewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the visibility items should be shown to the user or not.
		/// </summary>
		public bool VisibilityIsEnabled
		{
			get => (bool)this.GetValue(VisibilityIsEnabledProperty);
			set => this.SetValue(VisibilityIsEnabledProperty, value);
		}

		/// <summary>
		/// The different roles available to choose from when creating a contract.
		/// </summary>
		public ObservableCollection<string> AvailableRoles { get; }

		/// <summary>
		/// See <see cref="SelectedRole"/>
		/// </summary>
		public static readonly BindableProperty SelectedRoleProperty =
			BindableProperty.Create(nameof(SelectedRole), typeof(string), typeof(NewContractViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				NewContractViewModel viewModel = (NewContractViewModel)b;
				string oldRole = (string)oldValue;
				viewModel.RemoveRole(oldRole, viewModel.TagProfile.LegalIdentity.Id);
				string newRole = (string)newValue;
				if (!(viewModel.template is null) && !string.IsNullOrWhiteSpace(newRole))
				{
					viewModel.AddRole(newRole, viewModel.TagProfile.LegalIdentity.Id).Wait();
				}
			});

		/// <summary>
		/// The role selected for the contract, if any.
		/// </summary>
		public string SelectedRole
		{
			get => (string)this.GetValue(SelectedRoleProperty);
			set => this.SetValue(SelectedRoleProperty, value);
		}

		/// <summary>
		/// See <see cref="Roles"/>
		/// </summary>
		public static readonly BindableProperty RolesProperty =
			BindableProperty.Create(nameof(Roles), typeof(StackLayout), typeof(NewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's roles.
		/// </summary>
		public StackLayout Roles
		{
			get => (StackLayout)this.GetValue(RolesProperty);
			set => this.SetValue(RolesProperty, value);
		}

		/// <summary>
		/// See <see cref="Parameters"/>
		/// </summary>
		public static readonly BindableProperty ParametersProperty =
			BindableProperty.Create(nameof(Parameters), typeof(StackLayout), typeof(NewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parameters.
		/// </summary>
		public StackLayout Parameters
		{
			get => (StackLayout)this.GetValue(ParametersProperty);
			set => this.SetValue(ParametersProperty, value);
		}

		/// <summary>
		/// See <see cref="HumanReadableText"/>
		/// </summary>
		public static readonly BindableProperty HumanReadableTextProperty =
			BindableProperty.Create(nameof(HumanReadableText), typeof(StackLayout), typeof(NewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's human readable text section.
		/// </summary>
		public StackLayout HumanReadableText
		{
			get => (StackLayout)this.GetValue(HumanReadableTextProperty);
			set => this.SetValue(HumanReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="HasRoles"/>
		/// </summary>
		public static readonly BindableProperty HasRolesProperty =
			BindableProperty.Create(nameof(HasRoles), typeof(bool), typeof(NewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has roles.
		/// </summary>
		public bool HasRoles
		{
			get => (bool)this.GetValue(HasRolesProperty);
			set => this.SetValue(HasRolesProperty, value);
		}

		/// <summary>
		/// See <see cref="HasParameters"/>
		/// </summary>
		public static readonly BindableProperty HasParametersProperty =
			BindableProperty.Create(nameof(HasParameters), typeof(bool), typeof(NewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has parameters.
		/// </summary>
		public bool HasParameters
		{
			get => (bool)this.GetValue(HasParametersProperty);
			set => this.SetValue(HasParametersProperty, value);
		}

		/// <summary>
		/// See <see cref="ParametersOk"/>
		/// </summary>
		public static readonly BindableProperty ParametersOkProperty =
			BindableProperty.Create(nameof(ParametersOk), typeof(bool), typeof(NewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has parameters.
		/// </summary>
		public bool ParametersOk
		{
			get => (bool)this.GetValue(ParametersOkProperty);
			set => this.SetValue(ParametersOkProperty, value);
		}

		/// <summary>
		/// See <see cref="HasHumanReadableText"/>
		/// </summary>
		public static readonly BindableProperty HasHumanReadableTextProperty =
			BindableProperty.Create(nameof(HasHumanReadableText), typeof(bool), typeof(NewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract is comprised of human readable text.
		/// </summary>
		public bool HasHumanReadableText
		{
			get => (bool)this.GetValue(HasHumanReadableTextProperty);
			set => this.SetValue(HasHumanReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="CanAddParts"/>
		/// </summary>
		public static readonly BindableProperty CanAddPartsProperty =
			BindableProperty.Create(nameof(CanAddParts), typeof(bool), typeof(NewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can add parts to a contract.
		/// </summary>
		public bool CanAddParts
		{
			get => (bool)this.GetValue(CanAddPartsProperty);
			set => this.SetValue(CanAddPartsProperty, value);
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

			this.CanAddParts = false;
			this.VisibilityIsEnabled = false;
			this.EvaluateCommands(this.ProposeCommand);
		}

		private void RemoveRole(string role, string legalId)
		{
			Label ToRemove = null;
			int State = 0;

			if (this.Roles is null)
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

		private async Task AddRole(string role, string legalId)
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
							Label = new Label
							{
								Text = await ContactInfo.GetFriendlyName(legalId, this.XmppService.Xmpp, this.TagProfile, this.SmartContracts),
								StyleId = legalId,
								HorizontalOptions = LayoutOptions.FillAndExpand,
								HorizontalTextAlignment = TextAlignment.Center,
								FontAttributes = FontAttributes.Bold
							};

							TapGestureRecognizer OpenLegalId = new();
							OpenLegalId.Tapped += this.LegalId_Tapped;

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
					await this.ContractOrchestratorService.OpenLegalIdentity(label.StyleId, AppResources.ForInclusionInContract);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void AddPartButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				if (sender is Button button)
				{
					this.saveStateWhileScanning = true;
					this.stateTemplateWhileScanning = this.template;

					TaskCompletionSource<ContactInfo> Selected = new();
					ContactListNavigationArgs Args = new(AppResources.AddContactToContract, Selected)
					{
						CanScanQrCode = true
					};

					await this.NavigationService.GoToAsync(nameof(MyContactsPage), Args);

					ContactInfo Contact = await Selected.Task;
					if (Contact is null)
						return;

					if (string.IsNullOrEmpty(Contact.LegalId))
						await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.SelectedContactCannotBeAdded);
					else
					{
						this.partsToAdd[button.StyleId] = Contact.LegalId;
						string settingsKey = PartSettingsPrefix + button.StyleId;
						await this.SettingsService.SaveState(settingsKey, Contact.LegalId);
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void Parameter_DateChanged(object sender, NullableDateChangedEventArgs e)
		{
			try
			{
				if (sender is not ExtendedDatePicker Picker || !this.parametersByName.TryGetValue(Picker.StyleId, out ParameterInfo ParameterInfo))
					return;

				if (ParameterInfo.Parameter is DateParameter DP)
				{
					if (e.NewDate is not null)
					{
						DP.Value = e.NewDate;
						Picker.BackgroundColor = Color.Default;
					}
					else
					{
						Picker.BackgroundColor = Color.Salmon;
						return;
					}
				}

				await this.ValidateParameters();
				await PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private async void Parameter_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				if (sender is not Entry Entry || !this.parametersByName.TryGetValue(Entry.StyleId, out ParameterInfo ParameterInfo))
					return;

				if (ParameterInfo.Parameter is StringParameter SP)
					SP.Value = e.NewTextValue;
				else if (ParameterInfo.Parameter is NumericalParameter NP)
				{
					if (decimal.TryParse(e.NewTextValue, out decimal d))
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
				else if (ParameterInfo.Parameter is BooleanParameter BP)
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
				else if (ParameterInfo.Parameter is DateTimeParameter DTP)
				{
					if (DateTime.TryParse(e.NewTextValue, out DateTime TP))
					{
						DTP.Value = TP;
						Entry.BackgroundColor = Color.Default;
					}
					else
					{
						Entry.BackgroundColor = Color.Salmon;
						return;
					}
				}
				else if (ParameterInfo.Parameter is TimeParameter TSP)
				{
					if (TimeSpan.TryParse(e.NewTextValue, out TimeSpan TS))
					{
						TSP.Value = TS;
						Entry.BackgroundColor = Color.Default;
					}
					else
					{
						Entry.BackgroundColor = Color.Salmon;
						return;
					}
				}

				await this.ValidateParameters();
				await PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private async void Parameter_CheckedChanged(object sender, CheckedChangedEventArgs e)
		{
			try
			{
				if (sender is not CheckBox CheckBox || !this.parametersByName.TryGetValue(CheckBox.StyleId, out ParameterInfo ParameterInfo))
					return;

				if (ParameterInfo.Parameter is BooleanParameter BP)
					BP.Value = e.Value;

				await this.ValidateParameters();
				await PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private async Task ValidateParameters()
		{
			Variables Variables = new();
			bool Ok = true;

			Variables["Duration"] = this.template.Duration;

			foreach (ParameterInfo P in this.parametersByName.Values)
				P.Parameter.Populate(Variables);

			foreach (ParameterInfo P in this.parametersByName.Values)
			{
				if (await P.Parameter.IsParameterValid(Variables))
				{
					if (!(P.Control is null))
						P.Control.BackgroundColor = Color.Default;
				}
				else
				{
					if (!(P.Control is null))
						P.Control.BackgroundColor = Color.Salmon;

					Ok = false;
				}
			}

			this.ParametersOk = Ok;

			this.EvaluateCommands(this.ProposeCommand);
		}

		private async Task Propose()
		{
			List<Part> Parts = new();
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
									await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.TheContractRequiresAtLeast_AddMoreParts, Min, Role));
									return;
								}

								if (Nr > Min)
								{
									await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.TheContractRequiresAtMost_RemoveParts, Max, Role));
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
							await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.YourContractContainsErrors);
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
						await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.ContractVisibilityMustBeSelected);
						return;
				}

				if (this.SelectedRole is null)
				{
					await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.ContractRoleMustBeSelected);
					return;
				}

				if (!await App.VerifyPin())
					return;

				Created = await this.XmppService.Contracts.CreateContract(this.templateId, Parts.ToArray(), this.template.Parameters,
					this.template.Visibility, ContractParts.ExplicitlyDefined, this.template.Duration ?? Duration.FromYears(1),
					this.template.ArchiveRequired ?? Duration.FromYears(1), this.template.ArchiveOptional ?? Duration.FromYears(1),
					null, null, false);

				Created = await this.XmppService.Contracts.SignContract(Created, this.SelectedRole, false);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
			finally
			{
				if (!(Created is null))
				{
					await this.NavigationService.GoToAsync(nameof(Pages.Contracts.ViewContract.ViewContractPage), new ViewContractNavigationArgs(Created, false)
					{
						ReturnRoute = "///" + nameof(MainPage)
					});
				}
			}
		}

		internal static void Populate(StackLayout Layout, string Xaml)
		{
			StackLayout xaml = new StackLayout().LoadFromXaml(Xaml);

			List<View> children = new();
			children.AddRange(xaml.Children);

			foreach (View Element in children)
				Layout.Children.Add(Element);
		}

		private async Task PopulateTemplateForm(bool FirstTime, ContractVisibility? Visibility)
		{
			this.ClearTemplate(true);

			if (this.template is null)
				return;

			await this.PopulateHumanReadableText();

			this.HasRoles = this.template.Roles.Length > 0;
			this.VisibilityIsEnabled = true;

			StackLayout rolesLayout = new();
			foreach (Role Role in this.template.Roles)
			{
				this.AvailableRoles.Add(Role.Name);

				rolesLayout.Children.Add(new Label
				{
					Text = Role.Name,
					Style = (Style)Application.Current.Resources["LeftAlignedHeading"],
					StyleId = Role.Name
				});

				Populate(rolesLayout, await Role.ToXamarinForms(this.template.DeviceLanguage(), this.template));

				if (Role.MinCount > 0)
				{
					Button button = new()
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

			StackLayout parametersLayout = new();
			if (template.Parameters.Length > 0)
			{
				parametersLayout.Children.Add(new Label
				{
					Text = AppResources.Parameters,
					Style = (Style)Application.Current.Resources["LeftAlignedHeading"]
				});
			}

			this.parametersByName.Clear();

			foreach (Parameter Parameter in this.template.Parameters)
			{
				if (Parameter is BooleanParameter BP)
				{
					CheckBox CheckBox = new()
					{
						StyleId = Parameter.Name,
						IsChecked = BP.Value.HasValue && BP.Value.Value,
						VerticalOptions = LayoutOptions.Center
					};

					StackLayout Layout = new()
					{
						Orientation = StackOrientation.Horizontal
					};

					Layout.Children.Add(CheckBox);
					Populate(Layout, await Parameter.ToXamarinForms(this.template.DeviceLanguage(), this.template));
					parametersLayout.Children.Add(Layout);

					CheckBox.CheckedChanged += Parameter_CheckedChanged;

					this.parametersByName[Parameter.Name] = new ParameterInfo(Parameter, CheckBox);
				}
				else if (Parameter is CalcParameter)
				{
					this.parametersByName[Parameter.Name] = new ParameterInfo(Parameter, null);
				}
				else if (Parameter is DateParameter DP)
				{
					Populate(parametersLayout, await Parameter.ToXamarinForms(this.template.DeviceLanguage(), this.template));

					ExtendedDatePicker Picker = new()
					{
						StyleId = Parameter.Name,
						NullableDate = Parameter.ObjectValue as DateTime?,
						Placeholder = Parameter.Guide,
						HorizontalOptions = LayoutOptions.FillAndExpand,
					};

					parametersLayout.Children.Add(Picker);

					Picker.NullableDateSelected += Parameter_DateChanged;

					this.parametersByName[Parameter.Name] = new ParameterInfo(Parameter, Picker);
				}
				else
				{
					Populate(parametersLayout, await Parameter.ToXamarinForms(this.template.DeviceLanguage(), this.template));

					Entry Entry = new()
					{
						StyleId = Parameter.Name,
						Text = Parameter.ObjectValue?.ToString(),
						Placeholder = Parameter.Guide,
						HorizontalOptions = LayoutOptions.FillAndExpand,
					};

					parametersLayout.Children.Add(Entry);

					Entry.TextChanged += Parameter_TextChanged;

					this.parametersByName[Parameter.Name] = new ParameterInfo(Parameter, Entry);
				}
			}

			this.Parameters = parametersLayout;
			this.HasParameters = this.Parameters.Children.Count > 0;

			if (FirstTime)
			{
				if (!(this.template.Parts is null))
				{
					foreach (Part Part in this.template.Parts)
					{
						if (this.TagProfile.LegalIdentity.Id == Part.LegalId)
							this.SelectedRole = Part.Role;
						else
							await this.AddRole(Part.Role, Part.LegalId);
					}
				}

				if (Visibility.HasValue)
					this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == Visibility.Value);
			}

			await this.ValidateParameters();
			this.EvaluateCommands(this.ProposeCommand);
		}

		private async Task PopulateHumanReadableText()
		{
			this.HumanReadableText = null;

			StackLayout humanReadableTextLayout = new();

			if (!(this.template is null))
				Populate(humanReadableTextLayout, await this.template.ToXamarinForms(this.template.DeviceLanguage()));

			this.HumanReadableText = humanReadableTextLayout;
			this.HasHumanReadableText = humanReadableTextLayout.Children.Count > 0;
		}

		private bool CanPropose()
		{
			return !(this.template is null) && this.ParametersOk;
		}
	}
}