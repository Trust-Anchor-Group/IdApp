using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Converters;
using IdApp.Extensions;
using IdApp.Pages.Contracts.ClientSignature;
using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Contracts.ServerSignature;
using IdApp.Pages.Contracts.ViewContract.ObjectModel;
using IdApp.Services.UI.Photos;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.ViewContract
{
	/// <summary>
	/// The view model to bind to for when displaying contracts.
	/// </summary>
	public class ViewContractViewModel : QrXmppViewModel
	{
		private bool isReadOnly;
		private readonly PhotosLoader photosLoader;
		private DateTime skipContractEvent = DateTime.MinValue;

		/// <summary>
		/// Creates an instance of the <see cref="ViewContractViewModel"/> class.
		/// </summary>
		protected internal ViewContractViewModel()
		{
			this.Photos = new ObservableCollection<Photo>();
			this.photosLoader = new PhotosLoader(this.Photos);

			this.ObsoleteContractCommand = new Command(async _ => await this.ObsoleteContract());
			this.DeleteContractCommand = new Command(async _ => await this.DeleteContract());
			this.ShowDetailsCommand = new Command(async _ => await this.ShowDetails());

			this.GeneralInformation = new ObservableCollection<PartModel>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out ViewContractNavigationArgs args))
			{
				this.Contract = args.Contract;
				this.isReadOnly = args.IsReadOnly;
				this.Role = args.Role;
				this.IsProposal = !string.IsNullOrEmpty(this.Role);
				this.Proposal = string.IsNullOrEmpty(args.Proposal) ? LocalizationResourceManager.Current["YouHaveReceivedAProposal"] : args.Proposal;
			}
			else
			{
				this.Contract = null;
				this.isReadOnly = true;
				this.Role = null;
				this.IsProposal = false;
			}

			this.XmppService.Contracts.ContractsClient.ContractUpdated += this.ContractsClient_ContractUpdatedOrSigned;
			this.XmppService.Contracts.ContractsClient.ContractSigned += this.ContractsClient_ContractUpdatedOrSigned;

			if (this.Contract is not null)
			{
				DateTime TP = this.XmppService.Contracts.GetTimeOfLastContractEvent(this.Contract.ContractId);
				if (DateTime.Now.Subtract(TP).TotalSeconds < 5)
					this.Contract = await this.XmppService.Contracts.GetContract(this.Contract.ContractId);

				await this.DisplayContract();
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.XmppService.Contracts.ContractsClient.ContractUpdated -= this.ContractsClient_ContractUpdatedOrSigned;
			this.XmppService.Contracts.ContractsClient.ContractSigned -= this.ContractsClient_ContractUpdatedOrSigned;

			this.ClearContract();

			await base.OnDispose();
		}

		private Task ContractsClient_ContractUpdatedOrSigned(object Sender, ContractReferenceEventArgs e)
		{
			if (e.ContractId == this.Contract.ContractId && DateTime.Now.Subtract(this.skipContractEvent).TotalSeconds > 5)
				this.ReloadContract(e.ContractId);

			return Task.CompletedTask;
		}

		private async void ReloadContract(string ContractId)
		{
			try
			{
				Contract Contract = await this.XmppService.Contracts.GetContract(ContractId);
				
				this.UiSerializer.BeginInvokeOnMainThread(async () => await this.ContractUpdated(Contract));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private async Task ContractUpdated(Contract Contract)
		{
			this.ClearContract();

			this.Contract = Contract;

			if (this.Contract is not null)
				await this.DisplayContract();
		}

		#region Properties

		/// <summary>
		/// The command to bind to when marking a contract as obsolete.
		/// </summary>
		public ICommand ObsoleteContractCommand { get; }

		/// <summary>
		/// The command to bind to when deleting a contract.
		/// </summary>
		public ICommand DeleteContractCommand { get; }

		/// <summary>
		/// Command to show machine-readable details of contract.
		/// </summary>
		public ICommand ShowDetailsCommand { get; }

		/// <summary>
		/// See <see cref="Role"/>
		/// </summary>
		public static readonly BindableProperty RoleProperty =
			BindableProperty.Create(nameof(Role), typeof(string), typeof(ViewContractViewModel), default(string));

		/// <summary>
		/// Contains proposed role, if a proposal, null if not a proposal.
		/// </summary>
		public string Role
		{
			get => (string)this.GetValue(RoleProperty);
			set => this.SetValue(RoleProperty, value);
		}

		/// <summary>
		/// See <see cref="IsProposal"/>
		/// </summary>
		public static readonly BindableProperty IsProposalProperty =
			BindableProperty.Create(nameof(IsProposal), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// If the view represents a proposal to sign a contract.
		/// </summary>
		public bool IsProposal
		{
			get => (bool)this.GetValue(IsProposalProperty);
			set => this.SetValue(IsProposalProperty, value);
		}

		/// <summary>
		/// See <see cref="Proposal"/>
		/// </summary>
		public static readonly BindableProperty ProposalProperty =
			BindableProperty.Create(nameof(Proposal), typeof(string), typeof(ViewContractViewModel), default(string));

		/// <summary>
		/// If the contract is a proposal
		/// </summary>
		public string Proposal
		{
			get => (string)this.GetValue(ProposalProperty);
			set => this.SetValue(ProposalProperty, value);
		}

		/// <summary>
		/// Holds a list of general information sections for the contract.
		/// </summary>
		public ObservableCollection<PartModel> GeneralInformation { get; }

		/// <summary>
		/// See <see cref="Roles"/>
		/// </summary>
		public static readonly BindableProperty RolesProperty =
			BindableProperty.Create(nameof(Roles), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's roles.
		/// </summary>
		public StackLayout Roles
		{
			get => (StackLayout)this.GetValue(RolesProperty);
			set => this.SetValue(RolesProperty, value);
		}

		/// <summary>
		/// See <see cref="Parts"/>
		/// </summary>
		public static readonly BindableProperty PartsProperty =
			BindableProperty.Create(nameof(Parts), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parts.
		/// </summary>
		public StackLayout Parts
		{
			get => (StackLayout)this.GetValue(PartsProperty);
			set => this.SetValue(PartsProperty, value);
		}

		/// <summary>
		/// See <see cref="Parameters"/>
		/// </summary>
		public static readonly BindableProperty ParametersProperty =
			BindableProperty.Create(nameof(Parameters), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

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
			BindableProperty.Create(nameof(HumanReadableText), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's human readable text section.
		/// </summary>
		public StackLayout HumanReadableText
		{
			get => (StackLayout)this.GetValue(HumanReadableTextProperty);
			set => this.SetValue(HumanReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="MachineReadableText"/>
		/// </summary>
		public static readonly BindableProperty MachineReadableTextProperty =
			BindableProperty.Create(nameof(MachineReadableText), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's machine readable text section.
		/// </summary>
		public StackLayout MachineReadableText
		{
			get => (StackLayout)this.GetValue(MachineReadableTextProperty);
			set => this.SetValue(MachineReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="ClientSignatures"/>
		/// </summary>
		public static readonly BindableProperty ClientSignaturesProperty =
			BindableProperty.Create(nameof(ClientSignatures), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's client signatures.
		/// </summary>
		public StackLayout ClientSignatures
		{
			get => (StackLayout)this.GetValue(ClientSignaturesProperty);
			set => this.SetValue(ClientSignaturesProperty, value);
		}

		/// <summary>
		/// See <see cref="ServerSignatures"/>
		/// </summary>
		public static readonly BindableProperty ServerSignaturesProperty =
			BindableProperty.Create(nameof(ServerSignatures), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's server signatures.
		/// </summary>
		public StackLayout ServerSignatures
		{
			get => (StackLayout)this.GetValue(ServerSignaturesProperty);
			set => this.SetValue(ServerSignaturesProperty, value);
		}

		/// <summary>
		/// Gets the list of photos associated with the contract.
		/// </summary>
		public ObservableCollection<Photo> Photos { get; }

		/// <summary>
		/// The contract to display.
		/// </summary>
		public Contract Contract { get; private set; }

		/// <summary>
		/// See <see cref="HasPhotos"/>
		/// </summary>
		public static readonly BindableProperty HasPhotosProperty =
			BindableProperty.Create(nameof(HasPhotos), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether photos are available.
		/// </summary>
		public bool HasPhotos
		{
			get => (bool)this.GetValue(HasPhotosProperty);
			set => this.SetValue(HasPhotosProperty, value);
		}

		/// <summary>
		/// See <see cref="HasRoles"/>
		/// </summary>
		public static readonly BindableProperty HasRolesProperty =
			BindableProperty.Create(nameof(HasRoles), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any roles to display.
		/// </summary>
		public bool HasRoles
		{
			get => (bool)this.GetValue(HasRolesProperty);
			set => this.SetValue(HasRolesProperty, value);
		}

		/// <summary>
		/// See <see cref="HasParts"/>
		/// </summary>
		public static readonly BindableProperty HasPartsProperty =
			BindableProperty.Create(nameof(HasParts), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any contract parts to display.
		/// </summary>
		public bool HasParts
		{
			get => (bool)this.GetValue(HasPartsProperty);
			set => this.SetValue(HasPartsProperty, value);
		}

		/// <summary>
		/// See <see cref="HasParameters"/>
		/// </summary>
		public static readonly BindableProperty HasParametersProperty =
			BindableProperty.Create(nameof(HasParameters), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any parameters to display.
		/// </summary>
		public bool HasParameters
		{
			get => (bool)this.GetValue(HasParametersProperty);
			set => this.SetValue(HasParametersProperty, value);
		}

		/// <summary>
		/// See <see cref="HasHumanReadableText"/>
		/// </summary>
		public static readonly BindableProperty HasHumanReadableTextProperty =
			BindableProperty.Create(nameof(HasHumanReadableText), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any human readable texts to display.
		/// </summary>
		public bool HasHumanReadableText
		{
			get => (bool)this.GetValue(HasHumanReadableTextProperty);
			set => this.SetValue(HasHumanReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="HasMachineReadableText"/>
		/// </summary>
		public static readonly BindableProperty HasMachineReadableTextProperty =
			BindableProperty.Create(nameof(HasMachineReadableText), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any machine readable texts to display.
		/// </summary>
		public bool HasMachineReadableText
		{
			get => (bool)this.GetValue(HasMachineReadableTextProperty);
			set => this.SetValue(HasMachineReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="HasClientSignatures"/>
		/// </summary>
		public static readonly BindableProperty HasClientSignaturesProperty =
			BindableProperty.Create(nameof(HasClientSignatures), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any client signatures to display.
		/// </summary>
		public bool HasClientSignatures
		{
			get => (bool)this.GetValue(HasClientSignaturesProperty);
			set => this.SetValue(HasClientSignaturesProperty, value);
		}

		/// <summary>
		/// See <see cref="HasServerSignatures"/>
		/// </summary>
		public static readonly BindableProperty HasServerSignaturesProperty =
			BindableProperty.Create(nameof(HasServerSignatures), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any server signatures to display.
		/// </summary>
		public bool HasServerSignatures
		{
			get => (bool)this.GetValue(HasServerSignaturesProperty);
			set => this.SetValue(HasServerSignaturesProperty, value);
		}

		/// <summary>
		/// See <see cref="CanDeleteContract"/>
		/// </summary>
		public static readonly BindableProperty CanDeleteContractProperty =
			BindableProperty.Create(nameof(CanDeleteContract), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can delete or obsolete a contract.
		/// </summary>
		public bool CanDeleteContract
		{
			get => (bool)this.GetValue(CanDeleteContractProperty);
			set => this.SetValue(CanDeleteContractProperty, value);
		}

		/// <summary>
		/// See <see cref="CanObsoleteContract"/>
		/// </summary>
		public static readonly BindableProperty CanObsoleteContractProperty =
			BindableProperty.Create(nameof(CanObsoleteContract), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can delete or obsolete a contract.
		/// </summary>
		public bool CanObsoleteContract
		{
			get => (bool)this.GetValue(CanObsoleteContractProperty);
			set => this.SetValue(CanObsoleteContractProperty, value);
		}

		#endregion

		private void ClearContract()
		{
			this.photosLoader.CancelLoadPhotos();
			this.Contract = null;
			this.GeneralInformation.Clear();
			this.Roles = null;
			this.Parts = null;
			this.Parameters = null;
			this.HumanReadableText = null;
			this.MachineReadableText = null;
			this.ClientSignatures = null;
			this.ServerSignatures = null;
			this.HasPhotos = false;
			this.HasRoles = false;
			this.HasParts = false;
			this.HasParameters = false;
			this.HasHumanReadableText = false;
			this.HasMachineReadableText = false;
			this.HasClientSignatures = false;
			this.HasServerSignatures = false;
			this.CanDeleteContract = false;
			this.CanObsoleteContract = false;

			this.RemoveQrCode();
		}

		private async Task DisplayContract()
		{
			try
			{
				bool hasSigned = false;
				bool acceptsSignatures =
					(this.Contract.State == ContractState.Approved || this.Contract.State == ContractState.BeingSigned) &&
					(!this.Contract.SignAfter.HasValue || this.Contract.SignAfter.Value < DateTime.Now) &&
					(!this.Contract.SignBefore.HasValue || this.Contract.SignBefore.Value > DateTime.Now);
				Dictionary<string, int> nrSignatures = new();
				bool canObsolete = false;

				if (this.Contract.ClientSignatures is not null)
				{
					foreach (Waher.Networking.XMPP.Contracts.ClientSignature signature in this.Contract.ClientSignatures)
					{
						if (signature.LegalId == this.TagProfile.LegalIdentity.Id)
							hasSigned = true;

						if (!nrSignatures.TryGetValue(signature.Role, out int count))
							count = 0;

						nrSignatures[signature.Role] = count + 1;

						if (string.Compare(signature.BareJid, this.XmppService.BareJid, true) == 0)
						{
							if (this.Contract.Roles is not null)
							{
								foreach (Role Role in this.Contract.Roles)
								{
									if (Role.Name == signature.Role)
									{
										if (Role.CanRevoke)
										{
											canObsolete =
												this.Contract.State == ContractState.Approved ||
												this.Contract.State == ContractState.BeingSigned ||
												this.Contract.State == ContractState.Signed;
										}

										break;
									}
								}
							}
						}
					}
				}

				// General Information
				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["Created"], this.Contract.Created.ToString(CultureInfo.CurrentUICulture)));

				if (this.Contract.Updated > DateTime.MinValue)
					this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["Updated"], this.Contract.Updated.ToString(CultureInfo.CurrentUICulture)));

				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["State"], this.Contract.State.ToString(), ContractStateToColor.ToColor(this.Contract.State)));
				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["Visibility"], this.Contract.Visibility.ToString()));
				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["Duration"], this.Contract.Duration.ToString()));
				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["From"], this.Contract.From.ToString(CultureInfo.CurrentUICulture)));
				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["To"], this.Contract.To.ToString(CultureInfo.CurrentUICulture)));
				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["Archiving_Optional"], this.Contract.ArchiveOptional.ToString()));
				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["Archiving_Required"], this.Contract.ArchiveRequired.ToString()));
				this.GeneralInformation.Add(new PartModel(LocalizationResourceManager.Current["CanActAsTemplate"], this.Contract.CanActAsTemplate.ToYesNo()));

				this.GenerateQrCode(Constants.UriSchemes.CreateSmartContractUri(this.Contract.ContractId));

				// Roles
				if (this.Contract.Roles is not null)
				{
					StackLayout rolesLayout = new();
					foreach (Role role in this.Contract.Roles)
					{
						string html = await role.ToHTML(this.Contract.DeviceLanguage(), this.Contract);
						html = Waher.Content.Html.HtmlDocument.GetBody(html);

						AddKeyValueLabelPair(rolesLayout, role.Name, html + GenerateMinMaxCountString(role.MinCount, role.MaxCount), true, string.Empty, null);

						if (!this.isReadOnly && acceptsSignatures && !hasSigned && this.Contract.PartsMode == ContractParts.Open &&
							(!nrSignatures.TryGetValue(role.Name, out int count) || count < role.MaxCount) &&
							(!this.IsProposal || role.Name == this.Role))
						{
							Button button = new()
							{
								Text = string.Format(LocalizationResourceManager.Current["SignAsRole"], role.Name),
								StyleId = role.Name
							};

							button.Clicked += this.SignButton_Clicked;
							rolesLayout.Children.Add(button);
						}
					}
					this.Roles = rolesLayout;
				}

				// Parts
				StackLayout partsLayout = new();
				if (this.Contract.SignAfter.HasValue)
					AddKeyValueLabelPair(partsLayout, LocalizationResourceManager.Current["SignAfter"], this.Contract.SignAfter.Value.ToString(CultureInfo.CurrentUICulture));

				if (this.Contract.SignBefore.HasValue)
					AddKeyValueLabelPair(partsLayout, LocalizationResourceManager.Current["SignBefore"], this.Contract.SignBefore.Value.ToString(CultureInfo.CurrentUICulture));

				AddKeyValueLabelPair(partsLayout, LocalizationResourceManager.Current["Mode"], this.Contract.PartsMode.ToString());

				if (this.Contract.Parts is not null)
				{
					TapGestureRecognizer openLegalId = new();
					openLegalId.Tapped += this.Part_Tapped;

					foreach (Part part in this.Contract.Parts)
					{
						AddKeyValueLabelPair(partsLayout, part.Role, part.LegalId, false, part.LegalId, openLegalId);

						if (!this.isReadOnly && acceptsSignatures && !hasSigned && part.LegalId == this.TagProfile.LegalIdentity.Id)
						{
							Button button = new()
							{
								Text = string.Format(LocalizationResourceManager.Current["SignAsRole"], part.Role),
								StyleId = part.Role
							};

							button.Clicked += this.SignButton_Clicked;
							partsLayout.Children.Add(button);
						}
					}
				}
				this.Parts = partsLayout;

				// Parameters
				if (this.Contract.Parameters is not null)
				{
					StackLayout parametersLayout = new();

					foreach (Parameter parameter in this.Contract.Parameters)
					{
						if (parameter.ObjectValue is bool b)
							AddKeyValueLabelPair(parametersLayout, parameter.Name, b ? "✔" : "✗");
						else
							AddKeyValueLabelPair(parametersLayout, parameter.Name, parameter.ObjectValue?.ToString());
					}

					this.Parameters = parametersLayout;
				}

				// Human readable text
				StackLayout humanReadableTextLayout = new();
				string xaml = await this.Contract.ToXamarinForms(this.Contract.DeviceLanguage());
				StackLayout humanReadableXaml = new StackLayout().LoadFromXaml(xaml);
				List<View> children = new();
				children.AddRange(humanReadableXaml.Children);
				foreach (View view in children)
				{
					if (view is ContentView)
					{
						foreach (Element innView in (view as ContentView).Children)
						{
							if (innView is Label)
							{
								(innView as Label).TextColor = (Color) (Application.Current.RequestedTheme == OSAppTheme.Dark ?
								Application.Current.Resources["LabelTextColorDarkTheme"] : Application.Current.Resources["LabelTextColorLightTheme"]);
							}
						}
					}
					humanReadableTextLayout.Children.Add(view);
				}
				this.HumanReadableText = humanReadableTextLayout;

				// Machine readable text
				StackLayout machineReadableTextLayout = new();
				AddKeyValueLabelPair(machineReadableTextLayout, LocalizationResourceManager.Current["ContractId"], this.Contract.ContractId);
				if (!string.IsNullOrEmpty(this.Contract.TemplateId))
					AddKeyValueLabelPair(machineReadableTextLayout, LocalizationResourceManager.Current["TemplateId"], this.Contract.TemplateId);
				AddKeyValueLabelPair(machineReadableTextLayout, LocalizationResourceManager.Current["Digest"], Convert.ToBase64String(this.Contract.ContentSchemaDigest));
				AddKeyValueLabelPair(machineReadableTextLayout, LocalizationResourceManager.Current["HashFunction"], this.Contract.ContentSchemaHashFunction.ToString());
				AddKeyValueLabelPair(machineReadableTextLayout, LocalizationResourceManager.Current["LocalName"], this.Contract.ForMachinesLocalName);
				AddKeyValueLabelPair(machineReadableTextLayout, LocalizationResourceManager.Current["Namespace"], this.Contract.ForMachinesNamespace);
				this.MachineReadableText = machineReadableTextLayout;

				// Client signatures
				if (this.Contract.ClientSignatures is not null)
				{
					StackLayout clientSignaturesLayout = new();
					TapGestureRecognizer openClientSignature = new();
					openClientSignature.Tapped += this.ClientSignature_Tapped;

					foreach (Waher.Networking.XMPP.Contracts.ClientSignature signature in this.Contract.ClientSignatures)
					{
						string sign = Convert.ToBase64String(signature.DigitalSignature);
						AddKeyValueLabelPair(clientSignaturesLayout, signature.Role, signature.LegalId + ", " + signature.BareJid + ", " +
							signature.Timestamp.ToString(CultureInfo.CurrentUICulture) + ", " + sign, false, sign, openClientSignature);
					}

					this.ClientSignatures = clientSignaturesLayout;
				}

				// Server signature
				if (this.Contract.ServerSignature is not null)
				{
					StackLayout serverSignaturesLayout = new();
					TapGestureRecognizer openServerSignature = new();
					openServerSignature.Tapped += this.ServerSignature_Tapped;

					AddKeyValueLabelPair(serverSignaturesLayout, this.Contract.Provider, this.Contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture) + ", " +
						Convert.ToBase64String(this.Contract.ServerSignature.DigitalSignature), false, this.Contract.ContractId, openServerSignature);
					this.ServerSignatures = serverSignaturesLayout;
				}

				this.CanDeleteContract = !this.isReadOnly && !this.Contract.IsLegallyBinding(true);
				this.CanObsoleteContract = this.CanDeleteContract || canObsolete;

				this.HasRoles = this.Roles?.Children.Count > 0;
				this.HasParts = this.Parts?.Children.Count > 0;
				this.HasParameters = this.Parameters?.Children.Count > 0;
				this.HasHumanReadableText = this.HumanReadableText?.Children.Count > 0;
				this.HasMachineReadableText = this.MachineReadableText?.Children.Count > 0;
				this.HasClientSignatures = this.ClientSignatures?.Children.Count > 0;
				this.HasServerSignatures = this.ServerSignatures?.Children.Count > 0;

				if (this.Contract.Attachments is not null && this.Contract.Attachments.Length > 0)
				{
					_ = this.photosLoader.LoadPhotos(this.Contract.Attachments, SignWith.LatestApprovedId, () =>
						{
							this.UiSerializer.BeginInvokeOnMainThread(() => this.HasPhotos = this.Photos.Count > 0);
						});
				}
				else
					this.HasPhotos = false;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod())
					.Append(new KeyValuePair<string, object>("ContractId", this.Contract?.ContractId))
					.ToArray());

				this.ClearContract();
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private static string GenerateMinMaxCountString(int min, int max)
		{
			if (min == max)
			{
				if (max == 1)
					return string.Empty;
				return " (" + max.ToString() + ")";
			}

			return " (" + min.ToString() + " - " + max.ToString() + ")";
		}

		private static void AddKeyValueLabelPair(StackLayout container, string key, string value)
		{
			AddKeyValueLabelPair(container, key, value, false, string.Empty, null);
		}

		private static void AddKeyValueLabelPair(StackLayout container, string key, string value, bool isHtml, string styleId, TapGestureRecognizer tapGestureRecognizer)
		{
			StackLayout layout = new()
			{
				Orientation = StackOrientation.Horizontal,
				StyleId = styleId
			};

			container.Children.Add(layout);

			layout.Children.Add(new Label
			{
				Text = key + ":",
				Style = (Style)Application.Current.Resources["KeyLabel"]
			});

			layout.Children.Add(new Label
			{
				Text = value,
				TextType = isHtml ? TextType.Html : TextType.Text,
				Style = (Style)Application.Current.Resources[isHtml ? "FormattedValueLabel" : tapGestureRecognizer is null ? "ValueLabel" : "ClickableValueLabel"]
			});

			if (tapGestureRecognizer is not null)
				layout.GestureRecognizers.Add(tapGestureRecognizer);
		}

		private async void SignButton_Clicked(object Sender, EventArgs e)
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				if (Sender is Button button && !string.IsNullOrEmpty(button.StyleId))
				{
					this.skipContractEvent = DateTime.Now;

					Contract contract = await this.XmppService.Contracts.SignContract(this.Contract, button.StyleId, false);
					await this.ContractUpdated(contract);

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["ContractSuccessfullySigned"]);
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void Part_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
					await this.ContractOrchestratorService.OpenLegalIdentity(Layout.StyleId, LocalizationResourceManager.Current["PurposeReviewContract"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void ClientSignature_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
				{
					string sign = layout.StyleId;
					Waher.Networking.XMPP.Contracts.ClientSignature signature = this.Contract.ClientSignatures.FirstOrDefault(x => sign == Convert.ToBase64String(x.DigitalSignature));
					if (signature is not null)
					{
						string legalId = signature.LegalId;
						LegalIdentity identity = await this.XmppService.Contracts.GetLegalIdentity(legalId);

						await this.NavigationService.GoToAsync(nameof(Pages.Contracts.ClientSignature.ClientSignaturePage),
							new ClientSignatureNavigationArgs(signature, identity));
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void ServerSignature_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
				{
					await this.NavigationService.GoToAsync(nameof(Pages.Contracts.ServerSignature.ServerSignaturePage),
						  new ServerSignatureNavigationArgs(this.Contract));
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ObsoleteContract()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				this.skipContractEvent = DateTime.Now;

				Contract obsoletedContract = await this.XmppService.Contracts.ObsoleteContract(this.Contract.ContractId);
				await this.ContractUpdated(obsoletedContract);

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["ContractHasBeenObsoleted"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task DeleteContract()
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				this.skipContractEvent = DateTime.Now;

				Contract deletedContract = await this.XmppService.Contracts.DeleteContract(this.Contract.ContractId);
				await this.ContractUpdated(deletedContract);

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["ContractHasBeenDeleted"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ShowDetails()
		{
			try
			{
				byte[] Bin = Encoding.UTF8.GetBytes(this.Contract.ForMachines.OuterXml);
				HttpFileUploadEventArgs e = await this.XmppService.FileUploadClient.RequestUploadSlotAsync(this.Contract.ContractId + ".xml", "text/xml; charset=utf-8", Bin.Length);

				if (e.Ok)
				{
					await e.PUT(Bin, "text/xml", (int)Constants.Timeouts.UploadFile.TotalMilliseconds);
					await App.OpenUrl(e.GetUrl);
				}
				else
					await this.UiSerializer.DisplayAlert(e.StanzaError ?? new Exception(e.ErrorText));
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		#region ILinkableView

		/// <summary>
		/// Link to the current view
		/// </summary>
		public override string Link { get; }

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => ContractModel.GetName(this.Contract, this);

		#endregion

	}
}
