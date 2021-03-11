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
            this.photosLoader = new PhotosLoader(this.logService, networkService, this.neuronService, this.uiDispatcher, imageCacheService, this.Photos);
            this.ObsoleteContractCommand = new Command(async _ => await ObsoleteContract());
            this.DeleteContractCommand = new Command(async _ => await DeleteContract());
            this.GeneralInformation = new ObservableCollection<PartModel>();
            this.Roles = new StackLayout();
            this.Parts = new StackLayout();
            this.Parameters = new StackLayout();
            this.HumanReadableText = new StackLayout();
            this.MachineReadableText = new StackLayout();
            this.ClientSignatures = new StackLayout();
            this.ServerSignatures = new StackLayout();

        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();
            if (this.navigationService.TryPopArgs(out ViewContractNavigationArgs args))
            {
                this.Contract = args.Contract;
                this.isReadOnly = args.IsReadOnly;
            }
            else
            {
                this.Contract = null;
                this.isReadOnly = true;
            }
            if (this.Contract != null)
            {
                await LoadContract();
            }
        }

        /// <inheritdoc/>
        protected override async Task DoUnbind()
        {
            this.ClearContract();
            await base.DoUnbind();
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
        /// Holds a list of general information sections for the contract.
        /// </summary>
        public ObservableCollection<PartModel> GeneralInformation { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's roles.
        /// </summary>
        public StackLayout Roles { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's parts.
        /// </summary>
        public StackLayout Parts { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's parameters.
        /// </summary>
        public StackLayout Parameters { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's human readable text section.
        /// </summary>
        public StackLayout HumanReadableText { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's machine readable text section.
        /// </summary>
        public StackLayout MachineReadableText { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's client signatures.
        /// </summary>
        public StackLayout ClientSignatures { get; }

        /// <summary>
        /// Holds Xaml code for visually representing a contract's server signatures.
        /// </summary>
        public StackLayout ServerSignatures { get; }

        /// <summary>
        /// Gets the list of photos associated with the contract.
        /// </summary>
        public ObservableCollection<ImageSource> Photos { get; }

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
        /// See <see cref="CanDeleteOrObsoleteContract"/>
        /// </summary>
        public static readonly BindableProperty CanDeleteOrObsoleteContractProperty =
            BindableProperty.Create("CanDeleteOrObsoleteContract", typeof(bool), typeof(ViewContractViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether a user can delete or obsolete a contract.
        /// </summary>
        public bool CanDeleteOrObsoleteContract
        {
            get { return (bool)GetValue(CanDeleteOrObsoleteContractProperty); }
            set { SetValue(CanDeleteOrObsoleteContractProperty, value); }
        }

        #endregion

        private void ClearContract()
        {
            this.photosLoader.CancelLoadPhotos();
            this.HasPhotos = false;
            this.Contract = null;

            this.GeneralInformation.Clear();

            this.QrCode = null;

            this.Roles.Children.Clear();
            this.HasRoles = false;

            this.Parts.Children.Clear();
            this.HasParts = false;

            this.Parameters.Children.Clear();
            this.HasParameters = false;

            this.HumanReadableText.Children.Clear();
            this.HasHumanReadableText = false;

            this.MachineReadableText.Children.Clear();
            this.HasMachineReadableText = false;

            this.ClientSignatures.Children.Clear();
            this.HasClientSignatures = false;

            this.ServerSignatures.Children.Clear();
            this.HasServerSignatures = false;

            this.CanDeleteOrObsoleteContract = false;
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

                if (!(Contract.ClientSignatures is null))
                {
                    foreach (ClientSignature signature in Contract.ClientSignatures)
                    {
                        if (signature.LegalId == this.tagProfile.LegalIdentity.Id)
                            hasSigned = true;

                        if (!nrSignatures.TryGetValue(signature.Role, out int count))
                            count = 0;

                        nrSignatures[signature.Role] = count + 1;
                    }
                }

                // General Information
                this.GeneralInformation.Add(new PartModel(AppResources.Created, Contract.Created.ToString(CultureInfo.CurrentUICulture)));
                if (this.Contract.Updated > DateTime.MinValue)
                {
                    this.GeneralInformation.Add(new PartModel(AppResources.Created, Contract.Updated.ToString(CultureInfo.CurrentUICulture)));
                }
                this.GeneralInformation.Add(new PartModel(AppResources.State, Contract.State.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Visibility, Contract.Visibility.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Duration, Contract.Duration.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.From, Contract.From.ToString(CultureInfo.CurrentUICulture)));
                this.GeneralInformation.Add(new PartModel(AppResources.To, Contract.To.ToString(CultureInfo.CurrentUICulture)));
                this.GeneralInformation.Add(new PartModel(AppResources.Archiving_Optional, Contract.ArchiveOptional.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Archiving_Required, Contract.ArchiveRequired.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.CanActAsTemplate, Contract.CanActAsTemplate.ToYesNo()));

                // QR
                if (!(this.Contract is null))
                {
                    _ = Task.Run(() =>
                    {
                        byte[] bytes = QrCodeImageGenerator.GeneratePng(Constants.UriSchemes.CreateSmartContractUri(this.Contract.ContractId), this.QrCodeWidth, this.QrCodeHeight);
                        if (this.IsBound)
                        {
                            this.uiDispatcher.BeginInvokeOnMainThread(() => this.QrCode = ImageSource.FromStream(() => new MemoryStream(bytes)));
                        }
                    });
                }
                else
                {
                    this.QrCode = null;
                }

                // Roles
                if (!(Contract.Roles is null))
                {
                    foreach (Role role in Contract.Roles)
                    {
                        string html = role.ToHTML(Contract.DefaultLanguage, Contract);
                        html = Waher.Content.Html.HtmlDocument.GetBody(html);

                        AddKeyValueLabelPair(this.Roles, role.Name, html + GenerateMinMaxCountString(role.MinCount, role.MaxCount), true, string.Empty, null);

                        if (!this.isReadOnly && acceptsSignatures && !hasSigned && this.Contract.PartsMode == ContractParts.Open &&
                            (!nrSignatures.TryGetValue(role.Name, out int count) || count < role.MaxCount))
                        {
                            Button button = new Button
                            {
                                Text = string.Format(AppResources.SignAsRole, role.Name),
                                StyleId = role.Name
                            };
                            button.Clicked += SignButton_Clicked;
                            this.Roles.Children.Add(button);
                        }
                    }
                }

                // Parts
                if (Contract.SignAfter.HasValue)
                    AddKeyValueLabelPair(this.Parts, AppResources.SignAfter, Contract.SignAfter.Value.ToString(CultureInfo.CurrentUICulture));

                if (Contract.SignBefore.HasValue)
                    AddKeyValueLabelPair(this.Parts, AppResources.SignBefore, Contract.SignBefore.Value.ToString(CultureInfo.CurrentUICulture));

                AddKeyValueLabelPair(this.Parts, AppResources.Mode, Contract.PartsMode.ToString());

                if (!(Contract.Parts is null))
                {
                    TapGestureRecognizer openLegalId = new TapGestureRecognizer();
                    openLegalId.Tapped += this.Part_Tapped;

                    foreach (Part part in Contract.Parts)
                    {
                        AddKeyValueLabelPair(this.Parts, part.Role, part.LegalId, false, part.LegalId, openLegalId);

                        if (!this.isReadOnly && acceptsSignatures && !hasSigned && part.LegalId == this.tagProfile.LegalIdentity.Id)
                        {
                            Button button = new Button
                            {
                                Text = string.Format(AppResources.SignAsRole, part.Role),
                                StyleId = part.Role
                            };
                            button.Clicked += SignButton_Clicked;
                            this.Parts.Children.Add(button);
                        }
                    }
                }

                // Parameters
                if (!(Contract.Parameters is null))
                {
                    foreach (Parameter parameter in Contract.Parameters)
                        AddKeyValueLabelPair(this.Parameters, parameter.Name, parameter.ObjectValue?.ToString());
                }

                // Human readable text
                string xaml = Contract.ToXamarinForms(Contract.DefaultLanguage);
                StackLayout humanReadableXaml = new StackLayout().LoadFromXaml(xaml);
                List<View> children = new List<View>();
                children.AddRange(humanReadableXaml.Children);
                foreach (View view in children)
                    this.HumanReadableText.Children.Add(view);

                // Machine readable text
                AddKeyValueLabelPair(this.MachineReadableText, AppResources.ContractId, Contract.ContractId);
                if (!string.IsNullOrEmpty(Contract.TemplateId))
                    AddKeyValueLabelPair(this.MachineReadableText, AppResources.TemplateId, Contract.TemplateId);
                AddKeyValueLabelPair(this.MachineReadableText, AppResources.Digest, Convert.ToBase64String(Contract.ContentSchemaDigest));
                AddKeyValueLabelPair(this.MachineReadableText, AppResources.HashFunction, Contract.ContentSchemaHashFunction.ToString());
                AddKeyValueLabelPair(this.MachineReadableText, AppResources.LocalName, Contract.ForMachinesLocalName);
                AddKeyValueLabelPair(this.MachineReadableText, AppResources.Namespace, Contract.ForMachinesNamespace);

                // Client signatures
                if (!(Contract.ClientSignatures is null))
                {
                    TapGestureRecognizer openClientSignature = new TapGestureRecognizer();
                    openClientSignature.Tapped += this.ClientSignature_Tapped;

                    foreach (ClientSignature signature in Contract.ClientSignatures)
                    {
                        string sign = Convert.ToBase64String(signature.DigitalSignature);
                        AddKeyValueLabelPair(this.ClientSignatures, signature.Role, signature.LegalId + ", " + signature.BareJid + ", " +
                                                                      signature.Timestamp.ToString(CultureInfo.CurrentUICulture) + ", " + sign, false, sign, openClientSignature);
                    }
                }

                // Server signature
                if (!(Contract.ServerSignature is null))
                {
                    TapGestureRecognizer openServerSignature = new TapGestureRecognizer();
                    openServerSignature.Tapped += this.ServerSignature_Tapped;

                    AddKeyValueLabelPair(this.ServerSignatures, Contract.Provider, Contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture) + ", " +
                                                                    Convert.ToBase64String(Contract.ServerSignature.DigitalSignature), false, Contract.ContractId, openServerSignature);
                }

                this.CanDeleteOrObsoleteContract = !this.isReadOnly && !Contract.IsLegallyBinding(true);

                this.HasRoles = this.Roles.Children.Count > 0;
                this.HasParts = this.Parts.Children.Count > 0;
                this.HasParameters = this.Parameters.Children.Count > 0;
                this.HasHumanReadableText = this.HumanReadableText.Children.Count > 0;
                this.HasMachineReadableText = this.MachineReadableText.Children.Count > 0;
                this.HasClientSignatures = this.ClientSignatures.Children.Count > 0;
                this.HasServerSignatures = this.ServerSignatures.Children.Count > 0;

                if (!(this.Contract.Attachments is null))
                {
                    _ = this.photosLoader.LoadPhotos(this.Contract.Attachments, SignWith.LatestApprovedId, () =>
                        {
                            this.uiDispatcher.BeginInvokeOnMainThread(() => HasPhotos = this.Photos.Count > 0);
                        });
                }
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
                    this.uiDispatcher.BeginInvokeOnMainThread(() =>
                    {
                        this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(contract, false));
                    });
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

                await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(obsoletedContract, false));
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

                await this.navigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(deletedContract, false));
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex);
                await this.uiDispatcher.DisplayAlert(ex);
            }
        }
    }
}