using IdApp.Extensions;
using IdApp.Navigation.Contracts;
using IdApp.Services;
using IdApp.ViewModels.Contracts.ObjectModel;
using IdApp.Views.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.ViewModels.Contracts
{
	/// <summary>
	/// The view model to bind to for when displaying contracts.
	/// </summary>
	public class ViewContractViewModel : BaseViewModel
	{
		private bool isReadOnly;
		private readonly IUiDispatcher uiDispatcher;
		private readonly ILogService logService;
		private readonly INavigationService navigationService;
		private readonly INeuronService neuronService;
		private readonly IContractOrchestratorService contractOrchestratorService;
		private readonly ITagProfile tagProfile;
		private readonly PhotosLoader photosLoader;

		/// <summary>
		/// Creates an instance of the <see cref="ViewContractViewModel"/> class.
		/// </summary>
		public ViewContractViewModel()
			: this(null, null, null, null, null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ViewContractViewModel"/> class.
		/// For unit tests.
		/// <param name="tagProfile">The tag profile to work with.</param>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="logService">The log service.</param>
		/// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
		/// <param name="navigationService">The navigation service to use for app navigation</param>
		/// <param name="networkService">The network and connectivity service.</param>
		/// <param name="attachmentCacheService">The attachment cache to use.</param>
		/// <param name="contractOrchestratorService">The service to use for contract orchestration.</param>
		/// </summary>
		protected internal ViewContractViewModel(
			ITagProfile tagProfile,
			INeuronService neuronService,
			ILogService logService,
			IUiDispatcher uiDispatcher,
			INavigationService navigationService,
			INetworkService networkService,
			IAttachmentCacheService attachmentCacheService,
			IContractOrchestratorService contractOrchestratorService)
		{
			this.tagProfile = tagProfile ?? Types.Instantiate<ITagProfile>(false);
			this.neuronService = neuronService ?? Types.Instantiate<INeuronService>(false);
			this.logService = logService ?? Types.Instantiate<ILogService>(false);
			this.uiDispatcher = uiDispatcher ?? Types.Instantiate<IUiDispatcher>(false);
			this.navigationService = navigationService ?? Types.Instantiate<INavigationService>(false);
			networkService = networkService ?? Types.Instantiate<INetworkService>(false);
			this.contractOrchestratorService = contractOrchestratorService ?? Types.Instantiate<IContractOrchestratorService>(false);

			this.Photos = new ObservableCollection<Photo>();
			this.photosLoader = new PhotosLoader(this.logService, networkService, this.neuronService, this.uiDispatcher,
				attachmentCacheService ?? Types.Instantiate<IAttachmentCacheService>(false), this.Photos);

			this.ObsoleteContractCommand = new Command(async _ => await ObsoleteContract());
			this.DeleteContractCommand = new Command(async _ => await DeleteContract());
			this.GeneralInformation = new ObservableCollection<PartModel>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out ViewContractNavigationArgs args))
			{
				this.Contract = args.Contract;
				this.isReadOnly = args.IsReadOnly;
				this.IsProposal = args.IsProposal;
				this.Proposal = string.IsNullOrEmpty(args.Proposal) ? AppResources.YouHaveReceivedAProposal : args.Proposal;
			}
			else
			{
				this.Contract = null;
				this.isReadOnly = true;
				this.IsProposal = false;
			}

			if (this.Contract != null)
				await LoadContract();
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.ClearContract();
			await base.DoUnbind();
		}

		private async Task ContractUpdated(Contract Contract)
		{
			this.ClearContract();

			this.Contract = Contract;

			if (this.Contract != null)
				await LoadContract();
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
		/// See <see cref="IsProposal"/>
		/// </summary>
		public static readonly BindableProperty IsProposalProperty =
			BindableProperty.Create("IsProposal", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// If the contract is a proposal
		/// </summary>
		public bool IsProposal
		{
			get { return (bool)GetValue(IsProposalProperty); }
			set { SetValue(IsProposalProperty, value); }
		}

		/// <summary>
		/// See <see cref="Proposal"/>
		/// </summary>
		public static readonly BindableProperty ProposalProperty =
			BindableProperty.Create("Proposal", typeof(string), typeof(ViewContractViewModel), default(string));

		/// <summary>
		/// If the contract is a proposal
		/// </summary>
		public string Proposal
		{
			get { return (string)GetValue(ProposalProperty); }
			set { SetValue(ProposalProperty, value); }
		}

		/// <summary>
		/// Holds a list of general information sections for the contract.
		/// </summary>
		public ObservableCollection<PartModel> GeneralInformation { get; }

		/// <summary>
		/// See <see cref="Roles"/>
		/// </summary>
		public static readonly BindableProperty RolesProperty =
			BindableProperty.Create("Roles", typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's roles.
		/// </summary>
		public StackLayout Roles
		{
			get { return (StackLayout)GetValue(RolesProperty); }
			set { SetValue(RolesProperty, value); }
		}

		/// <summary>
		/// See <see cref="Parts"/>
		/// </summary>
		public static readonly BindableProperty PartsProperty =
			BindableProperty.Create("Parts", typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parts.
		/// </summary>
		public StackLayout Parts
		{
			get { return (StackLayout)GetValue(PartsProperty); }
			set { SetValue(PartsProperty, value); }
		}

		/// <summary>
		/// See <see cref="Parameters"/>
		/// </summary>
		public static readonly BindableProperty ParametersProperty =
			BindableProperty.Create("Parameters", typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

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
			BindableProperty.Create("HumanReadableText", typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's human readable text section.
		/// </summary>
		public StackLayout HumanReadableText
		{
			get { return (StackLayout)GetValue(HumanReadableTextProperty); }
			set { SetValue(HumanReadableTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="MachineReadableText"/>
		/// </summary>
		public static readonly BindableProperty MachineReadableTextProperty =
			BindableProperty.Create("MachineReadableText", typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's machine readable text section.
		/// </summary>
		public StackLayout MachineReadableText
		{
			get { return (StackLayout)GetValue(MachineReadableTextProperty); }
			set { SetValue(MachineReadableTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="ClientSignatures"/>
		/// </summary>
		public static readonly BindableProperty ClientSignaturesProperty =
			BindableProperty.Create("ClientSignatures", typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's client signatures.
		/// </summary>
		public StackLayout ClientSignatures
		{
			get { return (StackLayout)GetValue(ClientSignaturesProperty); }
			set { SetValue(ClientSignaturesProperty, value); }
		}

		/// <summary>
		/// See <see cref="ServerSignatures"/>
		/// </summary>
		public static readonly BindableProperty ServerSignaturesProperty =
			BindableProperty.Create("ServerSignatures", typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's server signatures.
		/// </summary>
		public StackLayout ServerSignatures
		{
			get { return (StackLayout)GetValue(ServerSignaturesProperty); }
			set { SetValue(ServerSignaturesProperty, value); }
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
			BindableProperty.Create("HasPhotos", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether photos are available.
		/// </summary>
		public bool HasPhotos
		{
			get { return (bool)GetValue(HasPhotosProperty); }
			set { SetValue(HasPhotosProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasRoles"/>
		/// </summary>
		public static readonly BindableProperty HasRolesProperty =
			BindableProperty.Create("HasRoles", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any roles to display.
		/// </summary>
		public bool HasRoles
		{
			get { return (bool)GetValue(HasRolesProperty); }
			set { SetValue(HasRolesProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasParts"/>
		/// </summary>
		public static readonly BindableProperty HasPartsProperty =
			BindableProperty.Create("HasParts", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any contract parts to display.
		/// </summary>
		public bool HasParts
		{
			get { return (bool)GetValue(HasPartsProperty); }
			set { SetValue(HasPartsProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasParameters"/>
		/// </summary>
		public static readonly BindableProperty HasParametersProperty =
			BindableProperty.Create("HasParameters", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any parameters to display.
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
			BindableProperty.Create("HasHumanReadableText", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any human readable texts to display.
		/// </summary>
		public bool HasHumanReadableText
		{
			get { return (bool)GetValue(HasHumanReadableTextProperty); }
			set { SetValue(HasHumanReadableTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasMachineReadableText"/>
		/// </summary>
		public static readonly BindableProperty HasMachineReadableTextProperty =
			BindableProperty.Create("HasMachineReadableText", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any machine readable texts to display.
		/// </summary>
		public bool HasMachineReadableText
		{
			get { return (bool)GetValue(HasMachineReadableTextProperty); }
			set { SetValue(HasMachineReadableTextProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasClientSignatures"/>
		/// </summary>
		public static readonly BindableProperty HasClientSignaturesProperty =
			BindableProperty.Create("HasClientSignatures", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any client signatures to display.
		/// </summary>
		public bool HasClientSignatures
		{
			get { return (bool)GetValue(HasClientSignaturesProperty); }
			set { SetValue(HasClientSignaturesProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasServerSignatures"/>
		/// </summary>
		public static readonly BindableProperty HasServerSignaturesProperty =
			BindableProperty.Create("HasServerSignatures", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any server signatures to display.
		/// </summary>
		public bool HasServerSignatures
		{
			get { return (bool)GetValue(HasServerSignaturesProperty); }
			set { SetValue(HasServerSignaturesProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCode"/>
		/// </summary>
		public static readonly BindableProperty QrCodeProperty =
			BindableProperty.Create("QrCode", typeof(ImageSource), typeof(ViewContractViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				ViewContractViewModel viewModel = (ViewContractViewModel)b;
				viewModel.HasQrCode = !(newValue is null);
			});

		/// <summary>
		/// Gets or sets the QR Code image.
		/// </summary>
		public ImageSource QrCode
		{
			get { return (ImageSource)GetValue(QrCodeProperty); }
			set { SetValue(QrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="HasQrCode"/>
		/// </summary>
		public static readonly BindableProperty HasQrCodeProperty =
			BindableProperty.Create("HasQrCode", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has a QR Code image for display.
		/// </summary>
		public bool HasQrCode
		{
			get { return (bool)GetValue(HasQrCodeProperty); }
			set { SetValue(HasQrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeWidth"/>
		/// </summary>
		public static readonly BindableProperty QrCodeWidthProperty =
			BindableProperty.Create("QrCodeWidth", typeof(int), typeof(ViewContractViewModel), UiConstants.QrCode.DefaultImageWidth);

		/// <summary>
		/// Gets or sets the width in pixels of the generated QR code image.
		/// </summary>
		public int QrCodeWidth
		{
			get { return (int)GetValue(QrCodeWidthProperty); }
			set { SetValue(QrCodeWidthProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeHeight"/>
		/// </summary>
		public static readonly BindableProperty QrCodeHeightProperty =
			BindableProperty.Create("QrCodeHeight", typeof(int), typeof(ViewContractViewModel), UiConstants.QrCode.DefaultImageHeight);

		/// <summary>
		/// Gets or sets the height in pixels of the generated QR code image.
		/// </summary>
		public int QrCodeHeight
		{
			get { return (int)GetValue(QrCodeHeightProperty); }
			set { SetValue(QrCodeHeightProperty, value); }
		}

		/// <summary>
		/// See <see cref="CanDeleteContract"/>
		/// </summary>
		public static readonly BindableProperty CanDeleteContractProperty =
			BindableProperty.Create("CanDeleteContract", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can delete or obsolete a contract.
		/// </summary>
		public bool CanDeleteContract
		{
			get { return (bool)GetValue(CanDeleteContractProperty); }
			set { SetValue(CanDeleteContractProperty, value); }
		}

		/// <summary>
		/// See <see cref="CanObsoleteContract"/>
		/// </summary>
		public static readonly BindableProperty CanObsoleteContractProperty =
			BindableProperty.Create("CanObsoleteContract", typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can delete or obsolete a contract.
		/// </summary>
		public bool CanObsoleteContract
		{
			get { return (bool)GetValue(CanObsoleteContractProperty); }
			set { SetValue(CanObsoleteContractProperty, value); }
		}

		#endregion

		private void ClearContract()
		{
			this.photosLoader.CancelLoadPhotos();
			this.Contract = null;
			this.GeneralInformation.Clear();
			this.QrCode = null;
			this.Roles = null;
			this.Parts = null;
			this.Parameters = null;
			this.HumanReadableText = null;
			this.MachineReadableText = null;
			this.ClientSignatures = null;
			this.ServerSignatures = null;
			this.HasPhotos = false;
			this.HasQrCode = false;
			this.HasRoles = false;
			this.HasParts = false;
			this.HasParameters = false;
			this.HasHumanReadableText = false;
			this.HasMachineReadableText = false;
			this.HasClientSignatures = false;
			this.HasServerSignatures = false;
			this.CanDeleteContract = false;
			this.CanObsoleteContract = false;
		}

		private async Task LoadContract()
		{
			try
			{
				bool hasSigned = false;
				bool acceptsSignatures =
					(Contract.State == ContractState.Approved || Contract.State == ContractState.BeingSigned) &&
					(!Contract.SignAfter.HasValue || Contract.SignAfter.Value < DateTime.Now) &&
					(!Contract.SignBefore.HasValue || Contract.SignBefore.Value > DateTime.Now);
				Dictionary<string, int> nrSignatures = new Dictionary<string, int>();
				bool canObsolete = false;

				if (!(Contract.ClientSignatures is null))
				{
					foreach (ClientSignature signature in Contract.ClientSignatures)
					{
						if (signature.LegalId == this.tagProfile.LegalIdentity.Id)
							hasSigned = true;

						if (!nrSignatures.TryGetValue(signature.Role, out int count))
							count = 0;

						nrSignatures[signature.Role] = count + 1;

						if (string.Compare(signature.BareJid, this.neuronService.BareJid, true) == 0)
						{
							if (!(Contract.Roles is null))
							{
								foreach (Role Role in Contract.Roles)
								{
									if (Role.Name == signature.Role)
									{
										if (Role.CanRevoke)
										{
											canObsolete =
												Contract.State == ContractState.Approved ||
												Contract.State == ContractState.BeingSigned ||
												Contract.State == ContractState.Signed;
										}

										break;
									}
								}
							}
						}
					}
				}

				// General Information
				this.GeneralInformation.Add(new PartModel(AppResources.Created, Contract.Created.ToString(CultureInfo.CurrentUICulture)));

				if (this.Contract.Updated > DateTime.MinValue)
					this.GeneralInformation.Add(new PartModel(AppResources.Created, Contract.Updated.ToString(CultureInfo.CurrentUICulture)));

				this.GeneralInformation.Add(new PartModel(AppResources.State, Contract.State.ToString()));
				this.GeneralInformation.Add(new PartModel(AppResources.Visibility, Contract.Visibility.ToString()));
				this.GeneralInformation.Add(new PartModel(AppResources.Duration, Contract.Duration.ToString()));
				this.GeneralInformation.Add(new PartModel(AppResources.From, Contract.From.ToString(CultureInfo.CurrentUICulture)));
				this.GeneralInformation.Add(new PartModel(AppResources.To, Contract.To.ToString(CultureInfo.CurrentUICulture)));
				this.GeneralInformation.Add(new PartModel(AppResources.Archiving_Optional, Contract.ArchiveOptional.ToString()));
				this.GeneralInformation.Add(new PartModel(AppResources.Archiving_Required, Contract.ArchiveRequired.ToString()));
				this.GeneralInformation.Add(new PartModel(AppResources.CanActAsTemplate, Contract.CanActAsTemplate.ToYesNo()));

				// QR
				_ = Task.Run(() =>
				{
					byte[] bytes = QrCodeImageGenerator.GeneratePng(Constants.UriSchemes.CreateSmartContractUri(this.Contract.ContractId), this.QrCodeWidth, this.QrCodeHeight);
					this.uiDispatcher.BeginInvokeOnMainThread(() =>
					{
						if (this.IsBound)
							this.QrCode = ImageSource.FromStream(() => new MemoryStream(bytes));
						else
							this.QrCode = null;
					});
				});

				// Roles
				if (!(Contract.Roles is null))
				{
					StackLayout rolesLayout = new StackLayout();
					foreach (Role role in Contract.Roles)
					{
						string html = role.ToHTML(Contract.DefaultLanguage, Contract);
						html = Waher.Content.Html.HtmlDocument.GetBody(html);

						AddKeyValueLabelPair(rolesLayout, role.Name, html + GenerateMinMaxCountString(role.MinCount, role.MaxCount), true, string.Empty, null);

						if (!this.isReadOnly && acceptsSignatures && !hasSigned && this.Contract.PartsMode == ContractParts.Open &&
							(!nrSignatures.TryGetValue(role.Name, out int count) || count < role.MaxCount))
						{
							Button button = new Button
							{
								Text = string.Format(AppResources.SignAsRole, role.Name),
								StyleId = role.Name
							};

							button.Clicked += SignButton_Clicked;
							rolesLayout.Children.Add(button);
						}
					}
					this.Roles = rolesLayout;
				}

				// Parts
				StackLayout partsLayout = new StackLayout();
				if (Contract.SignAfter.HasValue)
					AddKeyValueLabelPair(partsLayout, AppResources.SignAfter, Contract.SignAfter.Value.ToString(CultureInfo.CurrentUICulture));

				if (Contract.SignBefore.HasValue)
					AddKeyValueLabelPair(partsLayout, AppResources.SignBefore, Contract.SignBefore.Value.ToString(CultureInfo.CurrentUICulture));

				AddKeyValueLabelPair(partsLayout, AppResources.Mode, Contract.PartsMode.ToString());


				if (!(Contract.Parts is null))
				{
					TapGestureRecognizer openLegalId = new TapGestureRecognizer();
					openLegalId.Tapped += this.Part_Tapped;

					foreach (Part part in Contract.Parts)
					{
						AddKeyValueLabelPair(partsLayout, part.Role, part.LegalId, false, part.LegalId, openLegalId);

						if (!this.isReadOnly && acceptsSignatures && !hasSigned && part.LegalId == this.tagProfile.LegalIdentity.Id)
						{
							Button button = new Button
							{
								Text = string.Format(AppResources.SignAsRole, part.Role),
								StyleId = part.Role
							};

							button.Clicked += SignButton_Clicked;
							partsLayout.Children.Add(button);
						}
					}
				}
				this.Parts = partsLayout;

				// Parameters
				if (!(Contract.Parameters is null))
				{
					StackLayout parametersLayout = new StackLayout();
					foreach (Parameter parameter in Contract.Parameters)
						AddKeyValueLabelPair(parametersLayout, parameter.Name, parameter.ObjectValue?.ToString());
					this.Parameters = parametersLayout;
				}

				// Human readable text
				StackLayout humanReadableTextLayout = new StackLayout();
				string xaml = Contract.ToXamarinForms(Contract.DefaultLanguage);
				StackLayout humanReadableXaml = new StackLayout().LoadFromXaml(xaml);
				List<View> children = new List<View>();
				children.AddRange(humanReadableXaml.Children);
				foreach (View view in children)
					humanReadableTextLayout.Children.Add(view);
				this.HumanReadableText = humanReadableTextLayout;

				// Machine readable text
				StackLayout machineReadableTextLayout = new StackLayout();
				AddKeyValueLabelPair(machineReadableTextLayout, AppResources.ContractId, Contract.ContractId);
				if (!string.IsNullOrEmpty(Contract.TemplateId))
					AddKeyValueLabelPair(machineReadableTextLayout, AppResources.TemplateId, Contract.TemplateId);
				AddKeyValueLabelPair(machineReadableTextLayout, AppResources.Digest, Convert.ToBase64String(Contract.ContentSchemaDigest));
				AddKeyValueLabelPair(machineReadableTextLayout, AppResources.HashFunction, Contract.ContentSchemaHashFunction.ToString());
				AddKeyValueLabelPair(machineReadableTextLayout, AppResources.LocalName, Contract.ForMachinesLocalName);
				AddKeyValueLabelPair(machineReadableTextLayout, AppResources.Namespace, Contract.ForMachinesNamespace);
				this.MachineReadableText = machineReadableTextLayout;

				// Client signatures
				if (!(Contract.ClientSignatures is null))
				{
					StackLayout clientSignaturesLayout = new StackLayout();
					TapGestureRecognizer openClientSignature = new TapGestureRecognizer();
					openClientSignature.Tapped += this.ClientSignature_Tapped;

					foreach (ClientSignature signature in Contract.ClientSignatures)
					{
						string sign = Convert.ToBase64String(signature.DigitalSignature);
						AddKeyValueLabelPair(clientSignaturesLayout, signature.Role, signature.LegalId + ", " + signature.BareJid + ", " +
							signature.Timestamp.ToString(CultureInfo.CurrentUICulture) + ", " + sign, false, sign, openClientSignature);
					}

					this.ClientSignatures = clientSignaturesLayout;
				}

				// Server signature
				if (!(Contract.ServerSignature is null))
				{
					StackLayout serverSignaturesLayout = new StackLayout();
					TapGestureRecognizer openServerSignature = new TapGestureRecognizer();
					openServerSignature.Tapped += this.ServerSignature_Tapped;

					AddKeyValueLabelPair(serverSignaturesLayout, Contract.Provider, Contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture) + ", " +
						Convert.ToBase64String(Contract.ServerSignature.DigitalSignature), false, Contract.ContractId, openServerSignature);
					this.ServerSignatures = serverSignaturesLayout;
				}

				this.CanDeleteContract = !this.isReadOnly && !Contract.IsLegallyBinding(true);
				this.CanObsoleteContract = this.CanDeleteContract || canObsolete;

				this.HasRoles = this.Roles?.Children.Count > 0;
				this.HasParts = this.Parts?.Children.Count > 0;
				this.HasParameters = this.Parameters?.Children.Count > 0;
				this.HasHumanReadableText = this.HumanReadableText?.Children.Count > 0;
				this.HasMachineReadableText = this.MachineReadableText?.Children.Count > 0;
				this.HasClientSignatures = this.ClientSignatures?.Children.Count > 0;
				this.HasServerSignatures = this.ServerSignatures?.Children.Count > 0;

				if (!(this.Contract.Attachments is null) && this.Contract.Attachments.Length > 0)
				{
					_ = this.photosLoader.LoadPhotos(this.Contract.Attachments, SignWith.LatestApprovedId, () =>
						{
							this.uiDispatcher.BeginInvokeOnMainThread(() => HasPhotos = this.Photos.Count > 0);
						});
				}
				else
					this.HasPhotos = false;
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod())
					.Append(new KeyValuePair<string, string>("ContractId", this.Contract?.ContractId))
					.ToArray());

				ClearContract();
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}

		private static string GenerateMinMaxCountString(int min, int max)
		{
			if (min == max)
			{
				if (max == 1)
					return string.Empty;
				return $" ({max})";
			}

			return $" ({min} - {max})";
		}

		private static void AddKeyValueLabelPair(StackLayout container, string key, string value)
		{
			AddKeyValueLabelPair(container, key, value, false, string.Empty, null);
		}

		private static void AddKeyValueLabelPair(StackLayout container, string key, string value, bool isHtml, string styleId, TapGestureRecognizer tapGestureRecognizer)
		{
			StackLayout layout = new StackLayout
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

			if (!(tapGestureRecognizer is null))
				layout.GestureRecognizers.Add(tapGestureRecognizer);
		}

		private async void SignButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				if (sender is Button button && !string.IsNullOrEmpty(button.StyleId))
				{
					Contract contract = await this.neuronService.Contracts.SignContract(this.Contract, button.StyleId, false);
					await this.uiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractSuccessfullySigned);

					await this.ContractUpdated(contract);
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}

		private async void Part_Tapped(object sender, EventArgs e)
		{
			try
			{
				if (sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
					await this.contractOrchestratorService.OpenLegalIdentity(Layout.StyleId, "Reviewing contract where you are part.");
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}

		private async void ClientSignature_Tapped(object sender, EventArgs e)
		{
			try
			{
				if (sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
				{
					string sign = layout.StyleId;
					ClientSignature signature = this.Contract.ClientSignatures.FirstOrDefault(x => sign == Convert.ToBase64String(x.DigitalSignature));
					if (!(signature is null))
					{
						string legalId = signature.LegalId;
						LegalIdentity identity = await this.neuronService.Contracts.GetLegalIdentity(legalId);

						await this.navigationService.GoToAsync(nameof(ClientSignaturePage), new ClientSignatureNavigationArgs(signature, identity));
					}
				}
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}

		private async void ServerSignature_Tapped(object sender, EventArgs e)
		{
			try
			{
				if (sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
					await this.navigationService.GoToAsync(nameof(ServerSignaturePage), new ServerSignatureNavigationArgs(this.Contract));
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}

		private async Task ObsoleteContract()
		{
			try
			{
				Contract obsoletedContract = await this.neuronService.Contracts.ObsoleteContract(this.Contract.ContractId);

				await this.uiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractHasBeenObsoleted);

				await this.ContractUpdated(obsoletedContract);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}

		private async Task DeleteContract()
		{
			try
			{
				Contract deletedContract = await this.neuronService.Contracts.DeleteContract(this.Contract.ContractId);

				await this.uiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractHasBeenDeleted);

				await this.ContractUpdated(deletedContract);
			}
			catch (Exception ex)
			{
				this.logService.LogException(ex);
				await this.uiDispatcher.DisplayAlert(ex);
			}
		}
	}
}