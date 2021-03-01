using IdApp.Extensions;
using IdApp.Models;
using IdApp.Navigation;
using IdApp.Services;
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
using Xamarin.Forms;

namespace IdApp.ViewModels.Contracts
{
    /// <summary>
    /// The view model to bind to for when displaying contracts.
    /// </summary>
    public class ViewContractViewModel : BaseViewModel
    {
        private Contract contract;
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
        /// <param name="imageCacheService">The image cache to use.</param>
        /// <param name="contractOrchestratorService">The service to use for contract orchestration.</param>
        /// </summary>
        protected internal ViewContractViewModel(
            ITagProfile tagProfile,
            INeuronService neuronService, 
            ILogService logService, 
            IUiDispatcher uiDispatcher,
            INavigationService navigationService,
            INetworkService networkService,
            IImageCacheService imageCacheService,
            IContractOrchestratorService contractOrchestratorService)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<ITagProfile>();
            this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            this.logService = logService ?? DependencyService.Resolve<ILogService>();
            this.uiDispatcher = uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            networkService = networkService ?? DependencyService.Resolve<INetworkService>();
            imageCacheService = imageCacheService ?? DependencyService.Resolve<IImageCacheService>();
            this.contractOrchestratorService = contractOrchestratorService ?? DependencyService.Resolve<IContractOrchestratorService>();

            this.Photos = new ObservableCollection<ImageSource>();
            this.photosLoader = new PhotosLoader(this.logService,networkService, this.neuronService, this.uiDispatcher, imageCacheService, this.Photos);
            this.DisplayPartCommand = new Command<string>(async legalId => await ShowLegalId(legalId));
            this.SignPartAsRoleCommand = new Command<string>(async roleId => await SignContract(roleId));
            this.DisplayClientSignatureCommand = new Command<string>(async sign => await ShowClientSignature(sign));
            this.DisplayServerSignatureCommand = new Command(async () => await ShowServerSignature());
            this.ObsoleteContractCommand = new Command(async _ => await ObsoleteContract());
            this.DeleteContractCommand = new Command(async _ => await DeleteContract());
            this.GeneralInformation = new ObservableCollection<PartModel>();
            this.ContractParts = new ObservableCollection<PartModel>();
            this.ContractRoles = new ObservableCollection<PartModel>();
            this.ContractParameters = new ObservableCollection<ParameterModel>();
            this.ContractHumanReadableText = new ObservableCollection<string>();
            this.ContractMachineReadableText = new ObservableCollection<PartModel>();
            this.ContractClientSignatures = new ObservableCollection<PartModel>();
            this.ContractServerSignatures = new ObservableCollection<PartModel>();
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();
            if (this.navigationService.TryPopArgs(out ViewContractNavigationArgs args))
            {
                this.contract = args.Contract;
                this.isReadOnly = args.IsReadOnly;
            }
            else
            {
                this.contract = null;
                this.isReadOnly = true;
            }
            await LoadContract();
        }

        /// <inheritdoc/>
        protected override async Task DoUnbind()
        {
            this.ClearContract();
            await base.DoUnbind();
        }

        #region Properties

        /// <summary>
        /// The command to bind to when displaying part of a contract
        /// </summary>
        public ICommand DisplayPartCommand { get; }

        /// <summary>
        /// The command to bind to when signing part of a contract
        /// </summary>
        public ICommand SignPartAsRoleCommand { get; }

        /// <summary>
        /// The command to bind to when displaying the client signature of a contract
        /// </summary>
        public ICommand DisplayClientSignatureCommand { get; }

        /// <summary>
        /// The command to bind to when displaying the server signature of a contract
        /// </summary>
        public ICommand DisplayServerSignatureCommand { get; }

        /// <summary>
        /// The command to bind to when marking a contract as obsolete.
        /// </summary>
        public ICommand ObsoleteContractCommand { get; }

        /// <summary>
        /// The command to bind to when deleting a contract.
        /// </summary>
        public ICommand DeleteContractCommand { get; }
        /// <summary>
        /// Holds a list of general information sections for the contract.
        /// </summary>
        public ObservableCollection<PartModel> GeneralInformation { get; }

        /// <summary>
        /// Holds a list of roles for the contract.
        /// </summary>
        public ObservableCollection<PartModel> ContractRoles { get; }

        /// <summary>
        /// Holds a list of parts for the contract.
        /// </summary>
        public ObservableCollection<PartModel> ContractParts { get; }

        /// <summary>
        /// Holds a list of parameters for the contract.
        /// </summary>
        public ObservableCollection<ParameterModel> ContractParameters { get; }

        /// <summary>
        /// Holds a list of human readable text for the contract.
        /// </summary>
        public ObservableCollection<string> ContractHumanReadableText { get; }

        /// <summary>
        /// Holds a list of machine readable texts for the contract.
        /// </summary>
        public ObservableCollection<PartModel> ContractMachineReadableText { get; }

        /// <summary>
        /// Holds a list of client signatures for the contract.
        /// </summary>
        public ObservableCollection<PartModel> ContractClientSignatures { get; }

        /// <summary>
        /// Holds a list of server signatures for the contract.
        /// </summary>
        public ObservableCollection<PartModel> ContractServerSignatures { get; }

        /// <summary>
        /// Gets the list of photos associated with the contract.
        /// </summary>
        public ObservableCollection<ImageSource> Photos { get; }

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
                viewModel.HasQrCode = newValue != null;
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
            get { return (bool) GetValue(HasQrCodeProperty); }
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
            get { return (int) GetValue(QrCodeWidthProperty); }
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
            get { return (int) GetValue(QrCodeHeightProperty); }
            set { SetValue(QrCodeHeightProperty, value); }
        }

        /// <summary>
        /// See <see cref="CanDeleteOrObsoleteContract"/>
        /// </summary>
        public static readonly BindableProperty CanDeleteOrObsoleteContractProperty =
            BindableProperty.Create("CanDeleteOrObsoleteContract", typeof(bool), typeof(ViewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether a user can delete or obsolete a contract.
        /// </summary>
        public bool CanDeleteOrObsoleteContract
        {
            get { return (bool) GetValue(CanDeleteOrObsoleteContractProperty); }
            set { SetValue(CanDeleteOrObsoleteContractProperty, value); }
        }

        #endregion

        private void ClearContract()
        {
            this.photosLoader.CancelLoadPhotos();
            this.contract = null;
            this.GeneralInformation.Clear();
            this.ContractParameters.Clear();
            this.ContractRoles.Clear();
            this.ContractHumanReadableText.Clear();
            this.ContractMachineReadableText.Clear();
            this.ContractClientSignatures.Clear();
            this.ContractServerSignatures.Clear();
            this.HasRoles = false;
            this.HasParts = false;
            this.HasParameters = false;
            this.HasHumanReadableText = false;
            this.HasMachineReadableText = false;
            this.HasClientSignatures = false;
            this.HasServerSignatures = false;
            this.HasQrCode = false;
            this.CanDeleteOrObsoleteContract = false;
        }

        private async Task LoadContract()
        {
            try
            {
                // General Information
                this.GeneralInformation.Add(new PartModel(AppResources.Created, contract.Created.ToString(CultureInfo.CurrentUICulture)));
                if (this.contract.Updated > DateTime.MinValue)
                {
                    this.GeneralInformation.Add(new PartModel(AppResources.Created, contract.Updated.ToString(CultureInfo.CurrentUICulture)));
                }
                this.GeneralInformation.Add(new PartModel(AppResources.State, contract.State.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Visibility, contract.Visibility.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Duration, contract.Duration.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.From, contract.From.ToString(CultureInfo.CurrentUICulture)));
                this.GeneralInformation.Add(new PartModel(AppResources.To, contract.To.ToString(CultureInfo.CurrentUICulture)));
                this.GeneralInformation.Add(new PartModel(AppResources.Archiving_Optional, contract.ArchiveOptional.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Archiving_Required, contract.ArchiveRequired.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.CanActAsTemplate, contract.CanActAsTemplate.ToYesNo()));

                // QR
                if (this.contract != null)
                {
                    _ = Task.Run(() =>
                    {
                        byte[] png = QrCodeImageGenerator.GeneratePng(Constants.UriSchemes.CreateSmartContractUri(this.contract.ContractId), this.QrCodeWidth, this.QrCodeHeight);
                        if (this.IsBound)
                        {
                            this.uiDispatcher.BeginInvokeOnMainThread(() => this.QrCode = ImageSource.FromStream(() => new MemoryStream(png)));
                        }
                    });
                }
                else
                {
                    this.QrCode = null;
                }

                // Parts
                bool hasSigned = false;
                bool acceptsSignatures =
                    (contract.State == ContractState.Approved || contract.State == ContractState.BeingSigned) &&
                    (!contract.SignAfter.HasValue || contract.SignAfter.Value < DateTime.Now) &&
                    (!contract.SignBefore.HasValue || contract.SignBefore.Value > DateTime.Now);
                Dictionary<string, int> nrSignatures = new Dictionary<string, int>();

                if (contract.ClientSignatures != null)
                {
                    foreach (ClientSignature signature in contract.ClientSignatures)
                    {
                        if (signature.LegalId == this.tagProfile.LegalIdentity.Id)
                            hasSigned = true;

                        if (!nrSignatures.TryGetValue(signature.Role, out int count))
                            count = 0;

                        nrSignatures[signature.Role] = count + 1;
                    }
                }

                if (contract.SignAfter.HasValue)
                {
                    this.ContractParts.Add(new PartModel(AppResources.SignAfter, contract.SignAfter.Value.ToString(CultureInfo.CurrentUICulture)));
                }
                if (contract.SignBefore.HasValue)
                {
                    this.ContractParts.Add(new PartModel(AppResources.SignBefore, contract.SignBefore.Value.ToString(CultureInfo.CurrentUICulture)));
                }
                this.ContractParts.Add(new PartModel(AppResources.Mode, contract.PartsMode.ToString()));
                if (contract.Parts != null)
                {
                    foreach (Part part in contract.Parts)
                    {
                        PartModel model = new PartModel(part.Role, part.LegalId, part.LegalId);
                        if (!this.isReadOnly && acceptsSignatures && !hasSigned && part.LegalId == this.tagProfile.LegalIdentity.Id)
                        {
                            model.SignAsRole = part.Role;
                            model.SignAsRoleText = string.Format(AppResources.SignAsRole, part.Role);
                        }
                        this.ContractParts.Add(model);
                    }
                }

                // Roles
                if (this.contract.Roles != null)
                {
                    foreach (Role role in this.contract.Roles)
                    {
                        string html = role.ToHTML(contract.DefaultLanguage, contract);
                        html = Waher.Content.Html.HtmlDocument.GetBody(html);

                        PartModel model = new PartModel(role.Name, html + GenerateMinMaxCountString(role.MinCount, role.MaxCount))
                        {
                            IsHtml = true
                        };

                        if (!this.isReadOnly && acceptsSignatures && !hasSigned && this.contract.PartsMode == Waher.Networking.XMPP.Contracts.ContractParts.Open &&
                            (!nrSignatures.TryGetValue(role.Name, out int count) || count < role.MaxCount))
                        {
                            model.SignAsRole = role.Name;
                            model.SignAsRoleText = string.Format(AppResources.SignAsRole, role.Name);
                        }
                        this.ContractRoles.Add(model);
                    }
                }

                // Parameters
                if (contract.Parameters != null)
                {
                    foreach (Parameter parameter in contract.Parameters)
                    {
                        ParameterModel model = new ParameterModel(parameter.Name, parameter.ObjectValue?.ToString());
                        this.ContractParameters.Add(model);
                    }
                }

                // Human readable text
                // TODO: replace this with a data template selector
                //Populate(this.HumanReadableText, this.template.ToXamarinForms(this.template.DefaultLanguage));

                // Machine readable text
                this.ContractMachineReadableText.Add(new PartModel(AppResources.ContractId, contract.ContractId.ToString()));

                if (!string.IsNullOrEmpty(contract.TemplateId))
                    this.ContractMachineReadableText.Add(new PartModel(AppResources.TemplateId, contract.TemplateId));

                this.ContractMachineReadableText.Add(new PartModel(AppResources.Digest, Convert.ToBase64String(contract.ContentSchemaDigest)));
                this.ContractMachineReadableText.Add(new PartModel(AppResources.HashFunction, contract.ContentSchemaHashFunction.ToString()));
                this.ContractMachineReadableText.Add(new PartModel(AppResources.LocalName, contract.ForMachinesLocalName.ToString()));
                this.ContractMachineReadableText.Add(new PartModel(AppResources.Namespace, contract.ForMachinesNamespace.ToString()));

                // Client signatures
                if (contract.ClientSignatures != null)
                {
                    foreach (ClientSignature signature in contract.ClientSignatures)
                    {
                        string sign = Convert.ToBase64String(signature.DigitalSignature);
                        PartModel model = new PartModel(signature.Role, $"{signature.LegalId}, {signature.BareJid}, {signature.Timestamp.ToString(CultureInfo.CurrentUICulture)}, {sign}")
                        {
                            LegalId = sign
                        };
                        this.ContractClientSignatures.Add(model);
                    }
                }

                // Server signature
                if (contract.ServerSignature != null)
                {
                    string sign = Convert.ToBase64String(contract.ServerSignature.DigitalSignature);
                    PartModel model = new PartModel(contract.Provider, $"{contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture)}, {sign}");
                    this.ContractServerSignatures.Add(model);
                }

                this.CanDeleteOrObsoleteContract = !this.isReadOnly && !contract.IsLegallyBinding(true);

                this.HasRoles = this.ContractRoles.Count > 0;
                this.HasParts = this.ContractParts.Count > 0;
                this.HasParameters = this.ContractParameters.Count > 0;
                this.HasHumanReadableText = this.ContractHumanReadableText.Count > 0;
                this.HasMachineReadableText = this.ContractMachineReadableText.Count > 0;
                this.HasClientSignatures = this.ContractClientSignatures.Count > 0;
                this.HasServerSignatures = this.ContractServerSignatures.Count > 0;

                if (this.contract.Attachments != null)
                {
                    _ = this.photosLoader.LoadPhotos(this.contract.Attachments, SignWith.LatestApprovedId);
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod())
                    .Append(new KeyValuePair<string, string>("ContractId", this.contract.ContractId))
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

        private async Task ShowLegalId(string legalId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(legalId))
                {
                    await this.contractOrchestratorService.OpenLegalIdentity(legalId, "For inclusion as part in a contract.");
                }
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task SignContract(string roleId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(roleId))
                {
                    Contract signedContract = await this.neuronService.Contracts.SignContract(this.contract, roleId, false);

                    await this.uiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractSuccessfullySigned);

                    await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(signedContract, false));
                }
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task ShowClientSignature(string sign)
        {
            try
            {
                ClientSignature signature = this.contract.ClientSignatures.FirstOrDefault(x => sign == Convert.ToBase64String(x.DigitalSignature));
                if (signature != null)
                {
                    string legalId = signature.LegalId;
                    LegalIdentity identity = await this.neuronService.Contracts.GetLegalIdentity(legalId);

                    await this.navigationService.GoToAsync(nameof(ClientSignaturePage), new ClientSignatureNavigationArgs(signature, identity));
                }
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task ShowServerSignature()
        {
            try
            {
                await this.navigationService.GoToAsync(nameof(ServerSignaturePage), new ServerSignatureNavigationArgs(this.contract));
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task ObsoleteContract()
        {
            try
            {
                Contract obsoletedContract = await this.neuronService.Contracts.ObsoleteContract(this.contract.ContractId);

                await this.uiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractHasBeenObsoleted);

                await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(obsoletedContract, false));
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }

        private async Task DeleteContract()
        {
            try
            {
                Contract deletedContract = await this.neuronService.Contracts.DeleteContract(this.contract.ContractId);

                await this.uiDispatcher.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractHasBeenDeleted);

                await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(deletedContract, false));
            }
            catch (Exception ex)
            {
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }
    }
}