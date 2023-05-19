using IdApp.Controls.Extended;
using IdApp.Extensions;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Contracts.NewContract.ObjectModel;
using IdApp.Pages.Contracts.ViewContract;
using IdApp.Pages.Main.Calculator;
using IdApp.Pages.Main.Main;
using IdApp.Resx;
using IdApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Script;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.NewContract
{
	/// <summary>
	/// The view model to bind to when displaying a new contract view or page.
	/// </summary>
	public class NewContractViewModel : BaseViewModel, ILinkableView
	{
		private static readonly string partSettingsPrefix = typeof(NewContractViewModel).FullName + ".Part_";

		private readonly SortedDictionary<CaseInsensitiveString, ParameterInfo> parametersByName = new();
		private readonly LinkedList<ParameterInfo> parametersInOrder = new();
		private Dictionary<CaseInsensitiveString, object> presetParameterValues = new();
		private CaseInsensitiveString[] suppressedProposalIds;
		private Contract template;
		private string templateId;
		private bool saveStateWhileScanning;
		private Contract stateTemplateWhileScanning;
		private readonly Dictionary<CaseInsensitiveString, string> partsToAdd;

		/// <summary>
		/// Creates an instance of the <see cref="NewContractViewModel"/> class.
		/// </summary>
		protected internal NewContractViewModel()
		{
			this.ContractVisibilityItems = new ObservableCollection<ContractVisibilityModel>();
			this.AvailableRoles = new ObservableCollection<string>();
			this.ParameterOptions = new ObservableCollection<ContractOption>();
			this.ProposeCommand = new Command(async _ => await this.Propose(), _ => this.CanPropose());
			this.partsToAdd = new Dictionary<CaseInsensitiveString, string>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			ContractVisibility? Visibility = null;

			if (this.NavigationService.TryPopArgs(out NewContractNavigationArgs args))
			{
				this.template = args.Template;
				this.suppressedProposalIds = args.SuppressedProposalLegalIds;

				if (args.ParameterValues is not null)
					this.presetParameterValues = args.ParameterValues;

				if (args.ViewInitialized)
					Visibility = this.template?.Visibility;
				else
				{
					if (args.SetVisibility)
						Visibility = args.Template.Visibility;

					args.ViewInitialized = true;
				}
			}
			else if (this.stateTemplateWhileScanning is not null)
			{
				this.template = this.stateTemplateWhileScanning;
				this.stateTemplateWhileScanning = null;
			}

			this.templateId = this.template?.ContractId ?? string.Empty;
			this.IsTemplate = this.template?.CanActAsTemplate ?? false;

			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.CreatorAndParts, LocalizationResourceManager.Current["ContractVisibility_CreatorAndParts"]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.DomainAndParts, LocalizationResourceManager.Current["ContractVisibility_DomainAndParts"]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.Public, LocalizationResourceManager.Current["ContractVisibility_Public"]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.PublicSearchable, LocalizationResourceManager.Current["ContractVisibility_PublicSearchable"]));

			await this.PopulateTemplateForm(Visibility);
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.ContractVisibilityItems.Clear();

			this.ClearTemplate(false);

			if (!this.saveStateWhileScanning)
			{
				await this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)));
				await this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedRole)));
				await this.SettingsService.RemoveStateWhereKeyStartsWith(partSettingsPrefix);
			}

			await base.OnDispose();
		}

		/// <inheritdoc/>
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();

			if (this.SelectedContractVisibilityItem is not null)
				await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)), this.SelectedContractVisibilityItem.Visibility);
			else
				await this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)));

			if (this.SelectedRole is not null)
				await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.SelectedRole)), this.SelectedRole);
			else
				await this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedRole)));

			if (this.HasPartsToAdd)
			{
				foreach (KeyValuePair<CaseInsensitiveString, string> part in this.GetPartsToAdd())
				{
					string settingsKey = partSettingsPrefix + part.Key;
					await this.SettingsService.SaveState(settingsKey, part.Value);
				}
			}
			else
				await this.SettingsService.RemoveStateWhereKeyStartsWith(partSettingsPrefix);

			this.partsToAdd.Clear();
		}

		private bool HasPartsToAdd => this.partsToAdd.Count > 0;

		private KeyValuePair<CaseInsensitiveString, string>[] GetPartsToAdd()
		{
			int i = 0;
			int c = this.partsToAdd.Count;
			KeyValuePair<CaseInsensitiveString, string>[] Result = new KeyValuePair<CaseInsensitiveString, string>[c];

			foreach (KeyValuePair<CaseInsensitiveString, string> Part in this.partsToAdd)
				Result[i++] = Part;

			return Result;
		}

		/// <inheritdoc/>
		protected override async Task DoRestoreState()
		{
			if (this.saveStateWhileScanning)
			{
				Enum e = await this.SettingsService.RestoreEnumState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)));
				if (e is not null)
				{
					ContractVisibility cv = (ContractVisibility)e;
					this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == cv);
				}

				string selectedRole = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.SelectedRole)));
				string matchingRole = this.AvailableRoles.FirstOrDefault(x => x.Equals(selectedRole));

				if (!string.IsNullOrWhiteSpace(matchingRole))
					this.SelectedRole = matchingRole;

				List<(string key, string value)> settings = (await this.SettingsService.RestoreStateWhereKeyStartsWith<string>(partSettingsPrefix)).ToList();
				if (settings.Count > 0)
				{
					this.partsToAdd.Clear();
					foreach ((string key, string value) in settings)
					{
						string part = key[partSettingsPrefix.Length..];
						this.partsToAdd[part] = value;
					}
				}

				if (this.HasPartsToAdd)
				{
					foreach (KeyValuePair<CaseInsensitiveString, string> part in this.GetPartsToAdd())
						await this.AddRole(part.Key, part.Value);
				}

				await this.DeleteState();
			}

			this.saveStateWhileScanning = false;
			await base.DoRestoreState();
		}

		private async Task DeleteState()
		{
			await this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)));
			await this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedRole)));
			await this.SettingsService.RemoveStateWhereKeyStartsWith(partSettingsPrefix);
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
			set
			{
				this.SetValue(SelectedContractVisibilityItemProperty, value);

				if (this.template is not null && value is not null)
					this.template.Visibility = value.Visibility;
			}
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
		/// The different parameter options available to choose from when creating a contract.
		/// </summary>
		public ObservableCollection<ContractOption> ParameterOptions { get; }

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
				if (viewModel.template is not null && !string.IsNullOrWhiteSpace(newRole))
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

			if (this.template?.Parts is not null)
			{
				List<Part> Parts = new();

				foreach (Part Part in this.template.Parts)
				{
					if (Part.LegalId != legalId || Part.Role != role)
						Parts.Add(Part);
				}

				this.template.Parts = Parts.ToArray();
			}

			if (this.Roles is not null)
			{
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
								if (ToRemove is not null)
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
		}

		private async Task AddRole(string Role, string LegalId)
		{
			Contract contractToUse = this.template ?? this.stateTemplateWhileScanning;

			if ((contractToUse is null) || (this.Roles is null))
				return;

			Role RoleObj = null;

			foreach (Role R in contractToUse.Roles)
			{
				if (R.Name == Role)
				{
					RoleObj = R;
					break;
				}
			}

			if (RoleObj is null)
				return;

			if (this.template is not null)
			{
				List<Part> Parts = new();

				if (this.template.Parts is not null)
				{
					foreach (Part Part in this.template.Parts)
					{
						if (Part.LegalId != LegalId || Part.Role != Role)
							Parts.Add(Part);
					}
				}

				Parts.Add(new Part()
				{
					LegalId = LegalId,
					Role = Role
				});

				this.template.Parts = Parts.ToArray();
			}

			if (this.Roles is not null)
			{
				int NrParts = 0;
				int i = 0;
				bool CurrentRole = false;
				bool LegalIdAdded = false;

				foreach (View View in this.Roles.Children)
				{
					if (View is Label Label)
					{
						if (Label.StyleId == Role)
						{
							CurrentRole = true;
							NrParts = 0;
						}
						else
						{
							if (Label.StyleId == LegalId)
								LegalIdAdded = true;

							NrParts++;
						}
					}
					else if (View is Button Button)
					{
						if (CurrentRole)
						{
							if (!LegalIdAdded)
							{
								Label = new Label
								{
									Text = await ContactInfo.GetFriendlyName(LegalId, this),
									StyleId = LegalId,
									HorizontalOptions = LayoutOptions.FillAndExpand,
									HorizontalTextAlignment = TextAlignment.Center,
									FontAttributes = FontAttributes.Bold
								};

								TapGestureRecognizer OpenLegalId = new();
								OpenLegalId.Tapped += this.LegalId_Tapped;

								Label.GestureRecognizers.Add(OpenLegalId);

								this.Roles?.Children.Insert(i, Label);
								NrParts++;

								if (NrParts >= RoleObj.MaxCount)
									Button.IsEnabled = false;
							}

							return;
						}
						else
						{
							CurrentRole = false;
							LegalIdAdded = false;
							NrParts = 0;
						}
					}

					i++;
				}
			}
		}

		private async void LegalId_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label label && !string.IsNullOrEmpty(label.StyleId))
					await this.ContractOrchestratorService.OpenLegalIdentity(label.StyleId, LocalizationResourceManager.Current["ForInclusionInContract"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void AddPartButton_Clicked(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is Button button)
				{
					this.saveStateWhileScanning = true;
					this.stateTemplateWhileScanning = this.template;

					TaskCompletionSource<ContactInfoModel> Selected = new();
					ContactListNavigationArgs Args = new(LocalizationResourceManager.Current["AddContactToContract"], Selected)
					{
						CanScanQrCode = true,
						CancelReturnCounter = true
					};

					await this.NavigationService.GoToAsync(nameof(MyContactsPage), Args);

					ContactInfoModel Contact = await Selected.Task;
					if (Contact is null)
						return;

					if (string.IsNullOrEmpty(Contact.LegalId))
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SelectedContactCannotBeAdded"]);
					else
					{
						this.partsToAdd[button.StyleId] = Contact.LegalId;
						string settingsKey = partSettingsPrefix + button.StyleId;
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

		private async void Parameter_DateChanged(object Sender, NullableDateChangedEventArgs e)
		{
			try
			{
				if (Sender is not ExtendedDatePicker Picker || !this.parametersByName.TryGetValue(Picker.StyleId, out ParameterInfo ParameterInfo))
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
				await this.PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Parameter_TextChanged(object Sender, TextChangedEventArgs e)
		{
			try
			{
				if (Sender is not Entry Entry || !this.parametersByName.TryGetValue(Entry.StyleId, out ParameterInfo ParameterInfo))
					return;

				if (ParameterInfo.Parameter is StringParameter SP)
				{
					SP.Value = e.NewTextValue;
					Entry.BackgroundColor = Color.Default;
				}
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
					if (CommonTypes.TryParse(e.NewTextValue, out bool b))
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
				else if (ParameterInfo.Parameter is DurationParameter DP)
				{
					if (Duration.TryParse(e.NewTextValue, out Duration D))
					{
						DP.Value = D;
						Entry.BackgroundColor = Color.Default;
					}
					else
					{
						Entry.BackgroundColor = Color.Salmon;
						return;
					}
				}

				await this.ValidateParameters();
				await this.PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async void Parameter_CheckedChanged(object Sender, CheckedChangedEventArgs e)
		{
			try
			{
				if (Sender is not CheckBox CheckBox || !this.parametersByName.TryGetValue(CheckBox.StyleId, out ParameterInfo ParameterInfo))
					return;

				if (ParameterInfo.Parameter is BooleanParameter BP)
					BP.Value = e.Value;

				await this.ValidateParameters();
				await this.PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async Task ValidateParameters()
		{
			Variables Variables = new();
			bool Ok = true;

			if (this.template is not null)
				Variables["Duration"] = this.template.Duration;

			foreach (ParameterInfo P in this.parametersInOrder)
				P.Parameter.Populate(Variables);

			foreach (ParameterInfo P in this.parametersInOrder)
			{
				bool Valid;

				try
				{
					// Calculation parameters might only execute on the server. So, if they fail in the client, allow user to propose contract anyway.

					Valid = await P.Parameter.IsParameterValid(Variables) || P.Control is null;
				}
				catch (Exception)
				{
					Valid = false;
				}

				if (Valid)
				{
					if (P.Control is not null)
						P.Control.BackgroundColor = Color.Default;
				}
				else
				{
					if (P.Control is not null)
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
				if (this.Roles is not null)
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
										await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["TheContractRequiresAtLeast_AddMoreParts"], Min, Role));
										return;
									}

									if (Nr > Min)
									{
										await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["TheContractRequiresAtMost_RemoveParts"], Max, Role));
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
				}

				foreach (View View in this.Parameters.Children)
				{
					if (View is Entry Entry)
					{
						if (Entry.BackgroundColor == Color.Salmon)
						{
							await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["YourContractContainsErrors"]);
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
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["ContractVisibilityMustBeSelected"]);
						return;
				}

				if (this.SelectedRole is null)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["ContractRoleMustBeSelected"]);
					return;
				}

				if (!await App.VerifyPin())
					return;

				Created = await this.XmppService.CreateContract(this.templateId, Parts.ToArray(), this.template.Parameters,
					this.template.Visibility, ContractParts.ExplicitlyDefined, this.template.Duration ?? Duration.FromYears(1),
					this.template.ArchiveRequired ?? Duration.FromYears(1), this.template.ArchiveOptional ?? Duration.FromYears(1),
					null, null, false);

				Created = await this.XmppService.SignContract(Created, this.SelectedRole, false);

				if (Created.Parts is not null)
				{
					foreach (Part Part in Created.Parts)
					{
						if (this.suppressedProposalIds is not null && Array.IndexOf<CaseInsensitiveString>(this.suppressedProposalIds, Part.LegalId) >= 0)
							continue;

						ContactInfo Info = await ContactInfo.FindByLegalId(Part.LegalId);
						if (Info is null || string.IsNullOrEmpty(Info.BareJid))
							continue;

						string Proposal = await this.UiSerializer.DisplayPrompt(LocalizationResourceManager.Current["Proposal"],
							string.Format(LocalizationResourceManager.Current["EnterProposal"], Info.FriendlyName),
							LocalizationResourceManager.Current["Send"], LocalizationResourceManager.Current["Cancel"]);

						if (!string.IsNullOrEmpty(Proposal))
							this.XmppService.SendContractProposal(Created.ContractId, Part.Role, Info.BareJid, Proposal);
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
			finally
			{
				if (Created is not null)
				{
					await this.NavigationService.GoToAsync(nameof(ViewContractPage),
						new ViewContractNavigationArgs(Created, false) { ReturnRoute = "///" + nameof(MainPage) });
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

		private async Task PopulateTemplateForm(ContractVisibility? Visibility)
		{
			this.ClearTemplate(true);

			if (this.template is null)
				return;

			await this.PopulateHumanReadableText();

			this.HasRoles = (this.template.Roles?.Length ?? 0) > 0;
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
						Text = LocalizationResourceManager.Current["AddPart"],
						StyleId = Role.Name,
						Margin = (Thickness)Application.Current.Resources["DefaultBottomOnlyMargin"]
					};
					button.Clicked += this.AddPartButton_Clicked;

					rolesLayout.Children.Add(button);
				}
			}
			this.Roles = rolesLayout;

			StackLayout ParametersLayout = new();
			if (this.template.Parameters.Length > 0)
			{
				ParametersLayout.Children.Add(new Label
				{
					Text = LocalizationResourceManager.Current["Parameters"],
					Style = (Style)Application.Current.Resources["LeftAlignedHeading"]
				});
			}

			this.parametersByName.Clear();
			this.parametersInOrder.Clear();

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
					ParametersLayout.Children.Add(Layout);

					CheckBox.CheckedChanged += this.Parameter_CheckedChanged;

					ParameterInfo PI = new(Parameter, CheckBox);
					this.parametersByName[Parameter.Name] = PI;
					this.parametersInOrder.AddLast(PI);

					if (this.presetParameterValues.TryGetValue(Parameter.Name, out object PresetValue))
					{
						this.presetParameterValues.Remove(Parameter.Name);

						if (PresetValue is bool b || CommonTypes.TryParse(PresetValue?.ToString() ?? string.Empty, out b))
							CheckBox.IsChecked = b;
					}
				}
				else if (Parameter is CalcParameter)
				{
					ParameterInfo PI = new(Parameter, null);
					this.parametersByName[Parameter.Name] = PI;
					this.parametersInOrder.AddLast(PI);

					this.presetParameterValues.Remove(Parameter.Name);
				}
				else if (Parameter is DateParameter DP)
				{
					Populate(ParametersLayout, await Parameter.ToXamarinForms(this.template.DeviceLanguage(), this.template));

					ExtendedDatePicker Picker = new()
					{
						StyleId = Parameter.Name,
						NullableDate = Parameter.ObjectValue as DateTime?,
						Placeholder = Parameter.Guide,
						HorizontalOptions = LayoutOptions.FillAndExpand,
					};

					ParametersLayout.Children.Add(Picker);

					Picker.NullableDateSelected += this.Parameter_DateChanged;

					ParameterInfo PI = new(Parameter, Picker);
					this.parametersByName[Parameter.Name] = PI;
					this.parametersInOrder.AddLast(PI);

					if (this.presetParameterValues.TryGetValue(Parameter.Name, out object PresetValue))
					{
						this.presetParameterValues.Remove(Parameter.Name);

						if (PresetValue is DateTime TP || XML.TryParse(PresetValue?.ToString() ?? string.Empty, out TP))
							Picker.Date = TP;
					}
				}
				else
				{
					Populate(ParametersLayout, await Parameter.ToXamarinForms(this.template.DeviceLanguage(), this.template));

					Entry Entry = new()
					{
						StyleId = Parameter.Name,
						Text = Parameter.ObjectValue?.ToString(),
						Placeholder = Parameter.Guide,
						HorizontalOptions = LayoutOptions.FillAndExpand,
					};

					if (Parameter is NumericalParameter NP)
					{
						Grid Grid = new()
						{
							RowDefinitions = new RowDefinitionCollection()
							{
								new RowDefinition()
								{
									Height = GridLength.Auto
								}
							},
							ColumnDefinitions = new ColumnDefinitionCollection()
							{
								new ColumnDefinition()
								{
									Width = GridLength.Star
								},
								new ColumnDefinition()
								{
									Width = GridLength.Auto
								}
							},
							RowSpacing = 0,
							ColumnSpacing = 0,
							Padding = new Thickness(0),
							Margin = new Thickness(0)
						};

						Grid.SetColumn(Entry, 0);
						Grid.SetRow(Entry, 0);
						Grid.Children.Add(Entry);

						Button CalcButton = new()
						{
							FontFamily = (OnPlatform<string>)Application.Current.Resources["FontAwesomeSolid"],
							TextColor = (Color)Application.Current.Resources["TextColorLightTheme"],
							Text = FontAwesome.Calculator,
							HorizontalOptions = LayoutOptions.End,
							WidthRequest = 40,
							HeightRequest = 40,
							CornerRadius = 8,
							StyleId = Parameter.Name
						};

						CalcButton.Clicked += this.CalcButton_Clicked;

						Grid.SetColumn(CalcButton, 1);
						Grid.SetRow(CalcButton, 0);
						Grid.Children.Add(CalcButton);

						ParametersLayout.Children.Add(Grid);
					}
					else
						ParametersLayout.Children.Add(Entry);

					Entry.TextChanged += this.Parameter_TextChanged;

					ParameterInfo PI = new(Parameter, Entry);
					this.parametersByName[Parameter.Name] = PI;
					this.parametersInOrder.AddLast(PI);

					if (this.presetParameterValues.TryGetValue(Parameter.Name, out object PresetValue))
					{
						this.presetParameterValues.Remove(Parameter.Name);
						Entry.Text = PresetValue.ToString();
					}
				}
			}

			this.Parameters = ParametersLayout;
			this.HasParameters = this.Parameters.Children.Count > 0;

			if (this.template.Parts is not null)
			{
				foreach (Part Part in this.template.Parts)
				{
					if (this.TagProfile.LegalIdentity.Id == Part.LegalId)
						this.SelectedRole = Part.Role;
					else
						await this.AddRole(Part.Role, Part.LegalId);
				}
			}

			if (this.presetParameterValues.TryGetValue("Visibility", out object Obj) &&
				(Obj is ContractVisibility Visibility2 || Enum.TryParse(Obj?.ToString() ?? string.Empty, out Visibility2)))
			{
				Visibility = Visibility2;
				this.presetParameterValues.Remove("Visibility");
			}

			if (Visibility.HasValue)
				this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == Visibility.Value);

			if (this.HasRoles)
			{
				foreach (string Role in this.AvailableRoles)
				{
					if (this.presetParameterValues.TryGetValue(Role, out Obj) && Obj is string LegalId)
					{
						int i = LegalId.IndexOf('@');
						if (i < 0 || !Guid.TryParse(LegalId[..i], out _))
							continue;

						await this.AddRole(Role, LegalId);
						this.presetParameterValues.Remove(Role);
					}
					else if (this.template.Parts is not null)
					{
						foreach (Part Part in this.template.Parts)
						{
							if (Part.Role == Role)
								await this.AddRole(Part.Role, Part.LegalId);
						}
					}
				}
			}

			if (this.presetParameterValues.TryGetValue("Role", out Obj) && Obj is string SelectedRole)
			{
				this.SelectedRole = SelectedRole;
				this.presetParameterValues.Remove("Role");
			}

			await this.ValidateParameters();
			this.EvaluateCommands(this.ProposeCommand);
		}

		private async Task PopulateHumanReadableText()
		{
			this.HumanReadableText = null;

			StackLayout humanReadableTextLayout = new();

			if (this.template is not null)
				Populate(humanReadableTextLayout, await this.template.ToXamarinForms(this.template.DeviceLanguage()));

			this.HumanReadableText = humanReadableTextLayout;
			this.HasHumanReadableText = humanReadableTextLayout.Children.Count > 0;
		}

		private bool CanPropose()
		{
			return this.template is not null && this.ParametersOk;
		}

		private async void CalcButton_Clicked(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is not Button CalcButton)
					return;

				if (!this.parametersByName.TryGetValue(CalcButton.StyleId, out ParameterInfo ParameterInfo))
					return;

				if (ParameterInfo.Control is not Entry Entry)
					return;

				await this.NavigationService.GoToAsync(nameof(CalculatorPage), new CalculatorNavigationArgs(Entry)
				{
					CancelReturnCounter = true
				});
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		#region ILinkableView

		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		public bool IsLinkable => true;

		/// <summary>
		/// If App links should be encoded with the link.
		/// </summary>
		public bool EncodeAppLinks => true;

		/// <summary>
		/// Link to the current view
		/// </summary>
		public string Link
		{
			get
			{
				StringBuilder Url = new();
				bool First = true;

				Url.Append(Constants.UriSchemes.UriSchemeIotSc);
				Url.Append(':');
				Url.Append(this.template.ContractId);

				foreach (KeyValuePair<CaseInsensitiveString, ParameterInfo> P in this.parametersByName)
				{
					if (First)
					{
						First = false;
						Url.Append('&');
					}
					else
						Url.Append('?');

					Url.Append(P.Key);
					Url.Append('=');

					if (P.Value.Control is Entry Entry)
						Url.Append(Entry.Text);
					else if (P.Value.Control is CheckBox CheckBox)
						Url.Append(CheckBox.IsChecked ? '1' : '0');
					else if (P.Value.Control is ExtendedDatePicker Picker)
					{
						if (P.Value.Parameter is DateParameter)
							Url.Append(XML.Encode(Picker.Date, true));
						else
							Url.Append(XML.Encode(Picker.Date, false));
					}
					else
						P.Value.Parameter.ObjectValue?.ToString();
				}

				return Url.ToString();
			}
		}

		/// <summary>
		/// Title of the current view
		/// </summary>
		public Task<string> Title => ContractModel.GetName(this.template, this);

		/// <summary>
		/// If linkable view has media associated with link.
		/// </summary>
		public bool HasMedia => false;

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		public byte[] Media => null;

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		public string MediaContentType => null;

		#endregion

		#region Contract Options

		/// <summary>
		/// Method called (from main thread) when contract options are made available.
		/// </summary>
		/// <param name="Page">Page currently being viewed</param>
		/// <param name="Options">Available options, as dictionaries with contract parameters.</param>
		public void ShowContractOptions(NewContractPage Page, IDictionary<CaseInsensitiveString, object>[] Options)
		{
			if (Options.Length == 0)
				return;

			if (Options.Length == 1)
			{
				foreach (KeyValuePair<CaseInsensitiveString, object> Parameter in Options[0])
				{
					if (!this.parametersByName.TryGetValue(Parameter.Key, out ParameterInfo Info))
						continue;

					try
					{
						Info.Parameter.SetValue(Parameter.Value);

						if (Info.Control is Entry Entry)
							Entry.Text = Parameter.Value?.ToString() ?? string.Empty;
						else if (Info.Control is CheckBox CheckBox)
						{
							if (Parameter.Value is bool b)
								CheckBox.IsChecked = b;
							else if (Parameter.Value is int i)
								CheckBox.IsChecked = i != 0;
							else if (Parameter.Value is double d)
								CheckBox.IsChecked = d != 0;
							else if (Parameter.Value is decimal d2)
								CheckBox.IsChecked = d2 != 0;
							else if (Parameter.Value is string s && CommonTypes.TryParse(s, out b))
								CheckBox.IsChecked = b;
							else
							{
								this.LogService.LogWarning("Invalid option value.",
									new KeyValuePair<string, object>("Parameter", Parameter.Key),
									new KeyValuePair<string, object>("Value", Parameter.Value),
									new KeyValuePair<string, object>("Type", Parameter.Value?.GetType().FullName ?? string.Empty));
							}
						}
						else if (Info.Control is ExtendedDatePicker Picker)
						{
							if (Parameter.Value is DateTime TP)
								Picker.NullableDate = TP;
							else if (Parameter.Value is string s && (DateTime.TryParse(s, out TP) || XML.TryParse(s, out TP)))
								Picker.NullableDate = TP;
							else
							{
								this.LogService.LogWarning("Invalid option value.",
									new KeyValuePair<string, object>("Parameter", Parameter.Key),
									new KeyValuePair<string, object>("Value", Parameter.Value),
									new KeyValuePair<string, object>("Type", Parameter.Value?.GetType().FullName ?? string.Empty));
							}
						}
					}
					catch (Exception ex)
					{
						this.LogService.LogWarning("Invalid option value. Exception: " + ex.Message,
							new KeyValuePair<string, object>("Parameter", Parameter.Key),
							new KeyValuePair<string, object>("Value", Parameter.Value),
							new KeyValuePair<string, object>("Type", Parameter.Value?.GetType().FullName ?? string.Empty));

						continue;
					}
				}
			}
			else
			{
				CaseInsensitiveString PrimaryKey = this.GetPrimaryKey(Options);

				if (CaseInsensitiveString.IsNullOrEmpty(PrimaryKey))
				{
					this.LogService.LogWarning("Options not displayed. No primary key could be established.");
					return;
				}

				if (!this.parametersByName.TryGetValue(PrimaryKey, out ParameterInfo Info))
				{
					this.LogService.LogWarning("Options not displayed. Primary key not available in contract.");
					return;
				}

				if (Info.Control is not Entry Entry)
				{
					this.LogService.LogWarning("Options not displayed. Parameter control not of a type that allows a selection control to be created.");
					return;
				}

				int EntryIndex = this.Parameters.Children.IndexOf(Entry);
				if (EntryIndex < 0)
				{
					this.LogService.LogWarning("Options not displayed. Primary Key Entry not found.");
					return;
				}

				this.ParameterOptions.Clear();

				ContractOption SelectedOption = null;

				foreach (IDictionary<CaseInsensitiveString, object> Option in Options)
				{
					string Name = Option[PrimaryKey]?.ToString() ?? string.Empty;
					ContractOption ContractOption = new(Name, Option);

					this.ParameterOptions.Add(ContractOption);

					if (Name == Entry.Text)
						SelectedOption = ContractOption;
				}

				Picker Picker = new()
				{
					StyleId = Info.Parameter.Name,
					ItemsSource = this.ParameterOptions,
					Title = Info.Parameter.Guide,
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				this.Parameters.Children.RemoveAt(EntryIndex);
				this.Parameters.Children.Insert(EntryIndex, Picker);

				Picker.SelectedIndexChanged += this.Parameter_OptionSelectionChanged;
				Info.Control = Picker;

				if (SelectedOption is not null)
					Picker.SelectedItem = SelectedOption;
			}
		}

		private async void Parameter_OptionSelectionChanged(object Sender, EventArgs e)
		{
			if (Sender is not Picker Picker)
				return;

			if (Picker.SelectedItem is not ContractOption Option)
				return;

			try
			{
				foreach (KeyValuePair<CaseInsensitiveString, object> P in Option.Option)
				{
					try
					{
						if (!this.parametersByName.TryGetValue(P.Key, out ParameterInfo ParameterInfo))
							continue;

						Entry Entry = ParameterInfo.Control as Entry;

						if (ParameterInfo.Parameter is StringParameter SP)
						{
							string s = P.Value?.ToString() ?? string.Empty;

							SP.Value = s;

							if (Entry is not null)
							{
								Entry.Text = s;
								Entry.BackgroundColor = Color.Default;
							}
						}
						else if (ParameterInfo.Parameter is NumericalParameter NP)
						{
							try
							{
								NP.Value = Expression.ToDecimal(P.Value);

								if (Entry is not null)
									Entry.BackgroundColor = Color.Default;
							}
							catch (Exception)
							{
								if (Entry is not null)
									Entry.BackgroundColor = Color.Salmon;
							}
						}
						else if (ParameterInfo.Parameter is BooleanParameter BP)
						{
							CheckBox CheckBox = ParameterInfo.Control as CheckBox;

							try
							{
								if (P.Value is bool b2)
									BP.Value = b2;
								else if (P.Value is string s && CommonTypes.TryParse(s, out b2))
									BP.Value = b2;
								else
								{
									if (CheckBox is not null)
										CheckBox.BackgroundColor = Color.Salmon;

									continue;
								}

								if (CheckBox is not null)
									CheckBox.BackgroundColor = Color.Default;
							}
							catch (Exception)
							{
								if (CheckBox is not null)
									CheckBox.BackgroundColor = Color.Salmon;
							}
						}
						else if (ParameterInfo.Parameter is DateTimeParameter DTP)
						{
							Picker Picker2 = ParameterInfo.Control as Picker;

							if (P.Value is DateTime TP ||
								(P.Value is string s && (DateTime.TryParse(s, out TP) || XML.TryParse(s, out TP))))
							{
								DTP.Value = TP;

								if (Picker2 is not null)
									Picker2.BackgroundColor = Color.Default;
							}
							else
							{
								if (Picker2 is not null)
									Picker2.BackgroundColor = Color.Salmon;
							}
						}
						else if (ParameterInfo.Parameter is TimeParameter TSP)
						{
							if (P.Value is TimeSpan TS ||
								(P.Value is string s && TimeSpan.TryParse(s, out TS)))
							{
								TSP.Value = TS;

								if (Entry is not null)
									Entry.BackgroundColor = Color.Default;
							}
							else
							{
								if (Entry is not null)
									Entry.BackgroundColor = Color.Salmon;
							}
						}
						else if (ParameterInfo.Parameter is DurationParameter DP)
						{
							if (P.Value is Duration D ||
								(P.Value is string s && Duration.TryParse(s, out D)))
							{
								DP.Value = D;

								if (Entry is not null)
									Entry.BackgroundColor = Color.Default;
							}
							else
							{
								if (Entry is not null)
									Entry.BackgroundColor = Color.Salmon;

								return;
							}
						}
					}
					catch (Exception ex)
					{
						this.LogService.LogException(ex);
					}
				}

				await this.ValidateParameters();
				await this.PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private CaseInsensitiveString GetPrimaryKey(IDictionary<CaseInsensitiveString, object>[] Options)
		{
			Dictionary<CaseInsensitiveString, Dictionary<string, bool>> ByKeyAndValue = new();
			LinkedList<CaseInsensitiveString> Keys = new();
			int c = Options.Length;

			foreach (IDictionary<CaseInsensitiveString, object> Option in Options)
			{
				foreach (KeyValuePair<CaseInsensitiveString, object> P in Option)
				{
					if (!ByKeyAndValue.TryGetValue(P.Key, out Dictionary<string, bool> Values))
					{
						Values = new Dictionary<string, bool>();
						ByKeyAndValue[P.Key] = Values;
						Keys.AddLast(P.Key);
					}

					if (P.Value is string s)
						Values[s] = true;
				}
			}

			foreach (CaseInsensitiveString Key in Keys)
			{
				if (ByKeyAndValue[Key].Count == c &&
					this.parametersByName.TryGetValue(Key, out ParameterInfo Info) &&
					Info.Parameter is StringParameter)
				{
					return Key;
				}
			}

			return CaseInsensitiveString.Empty;
		}

		#endregion

	}
}
