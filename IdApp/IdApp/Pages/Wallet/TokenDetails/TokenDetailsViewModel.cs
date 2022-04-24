using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using NeuroFeatures.Tags;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Security;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of a token.
	/// </summary>
	public class TokenDetailsViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="EDalerUriViewModel"/> class.
		/// </summary>
		public TokenDetailsViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out TokenDetailsNavigationArgs args))
			{
				this.Created = args.Token.Created;
				this.Updated = args.Token.Updated;
				this.Expires = args.Token.Expires;
				this.ArchiveRequired = args.Token.ArchiveRequired;
				this.ArchiveOptional = args.Token.ArchiveOptional;
				this.SignatureTimestamp = args.Token.SignatureTimestamp;
				this.Signature = args.Token.Signature;
				this.DefinitionSchemaDigest = args.Token.DefinitionSchemaDigest;
				this.DefinitionSchemaHashFunction = args.Token.DefinitionSchemaHashFunction;
				this.CreatorCanDestroy = args.Token.CreatorCanDestroy;
				this.OwnerCanDestroyBatch = args.Token.OwnerCanDestroyBatch;
				this.OwnerCanDestroyIndividual = args.Token.OwnerCanDestroyIndividual;
				this.CertifierCanDestroy = args.Token.CertifierCanDestroy;
				this.FriendlyName = args.Token.FriendlyName;
				this.GlyphContentType = args.Token.GlyphContentType;
				this.Ordinal = args.Token.Ordinal;
				this.Value = args.Token.Value;
				this.Witness = args.Token.Witness;
				this.CertifierJids = args.Token.CertifierJids;
				this.Certifier = args.Token.Certifier;
				this.TokenIdMethod = args.Token.TokenIdMethod;
				this.TokenId = args.Token.TokenId;
				this.Visibility = args.Token.Visibility;
				this.Creator = args.Token.Creator;
				this.CreatorJid = args.Token.CreatorJid;
				this.Owner = args.Token.Owner;
				this.BatchSize = args.Token.BatchSize;
				this.TrustProvider = args.Token.TrustProvider;
				this.OwnerJid = args.Token.OwnerJid;
				this.Currency = args.Token.Currency;
				this.Reference = args.Token.Reference;
				this.Definition = args.Token.Definition;
				this.DefinitionNamespace = args.Token.DefinitionNamespace;
				this.CreationContract = args.Token.CreationContract;
				this.OwnershipContract = args.Token.OwnershipContract;
				this.Valuator = args.Token.Valuator;
				this.Assessor = args.Token.Assessor;
				this.TrustProviderJid = args.Token.TrustProviderJid;
				this.Tags = args.Token.Tags;
				this.GlyphImage = args.Token.GlyphImage;
				this.HasGlyphImage = args.Token.HasGlyphImage;
				this.GlyphWidth = args.Token.GlyphWidth;
				this.GlyphHeight = args.Token.GlyphHeight;
			}

			AssignProperties();
			EvaluateAllCommands();

			this.TagProfile.Changed += TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			this.TagProfile.Changed -= TagProfile_Changed;
			await base.DoUnbind();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
		}

		/// <inheritdoc/>
		protected override void XmppService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
				this.EvaluateAllCommands();
			});
		}

		private void TagProfile_Changed(object sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(AssignProperties);
		}

		#region Properties

		/// <summary>
		/// See <see cref="Created"/>
		/// </summary>
		public static readonly BindableProperty CreatedProperty =
			BindableProperty.Create(nameof(Created), typeof(DateTime), typeof(TokenDetailsViewModel), default(DateTime));

		/// <summary>
		/// When token was created.
		/// </summary>
		public DateTime Created
		{
			get => (DateTime)this.GetValue(CreatedProperty);
			set => this.SetValue(CreatedProperty, value);
		}

		/// <summary>
		/// See <see cref="Updated"/>
		/// </summary>
		public static readonly BindableProperty UpdatedProperty =
			BindableProperty.Create(nameof(Updated), typeof(DateTime), typeof(TokenDetailsViewModel), default(DateTime));

		/// <summary>
		/// When token was last updated.
		/// </summary>
		public DateTime Updated
		{
			get => (DateTime)this.GetValue(UpdatedProperty);
			set => this.SetValue(UpdatedProperty, value);
		}

		/// <summary>
		/// See <see cref="Expires"/>
		/// </summary>
		public static readonly BindableProperty ExpiresProperty =
			BindableProperty.Create(nameof(Expires), typeof(DateTime), typeof(TokenDetailsViewModel), default(DateTime));

		/// <summary>
		/// When token expires.
		/// </summary>
		public DateTime Expires
		{
			get => (DateTime)this.GetValue(ExpiresProperty);
			set => this.SetValue(ExpiresProperty, value);
		}

		/// <summary>
		/// See <see cref="ArchiveRequired"/>
		/// </summary>
		public static readonly BindableProperty ArchiveRequiredProperty =
			BindableProperty.Create(nameof(ArchiveRequired), typeof(Duration?), typeof(TokenDetailsViewModel), default(Duration?));

		/// <summary>
		/// Required archiving time after token expires.
		/// </summary>
		public Duration? ArchiveRequired
		{
			get => (Duration?)this.GetValue(ArchiveRequiredProperty);
			set => this.SetValue(ArchiveRequiredProperty, value);
		}

		/// <summary>
		/// See <see cref="ArchiveOptional"/>
		/// </summary>
		public static readonly BindableProperty ArchiveOptionalProperty =
			BindableProperty.Create(nameof(ArchiveOptional), typeof(Duration?), typeof(TokenDetailsViewModel), default(Duration?));

		/// <summary>
		/// Optional archiving time after required archiving time.
		/// </summary>
		public Duration? ArchiveOptional
		{
			get => (Duration?)this.GetValue(ArchiveOptionalProperty);
			set => this.SetValue(ArchiveOptionalProperty, value);
		}

		/// <summary>
		/// See <see cref="SignatureTimestamp"/>
		/// </summary>
		public static readonly BindableProperty SignatureTimestampProperty =
			BindableProperty.Create(nameof(SignatureTimestamp), typeof(DateTime), typeof(TokenDetailsViewModel), default(DateTime));

		/// <summary>
		/// Signature timestamp
		/// </summary>
		public DateTime SignatureTimestamp
		{
			get => (DateTime)this.GetValue(SignatureTimestampProperty);
			set => this.SetValue(SignatureTimestampProperty, value);
		}

		/// <summary>
		/// See <see cref="Signature"/>
		/// </summary>
		public static readonly BindableProperty SignatureProperty =
			BindableProperty.Create(nameof(Signature), typeof(byte[]), typeof(TokenDetailsViewModel), default(byte[]));

		/// <summary>
		/// Token signature
		/// </summary>
		public byte[] Signature
		{
			get => (byte[])this.GetValue(SignatureProperty);
			set => this.SetValue(SignatureProperty, value);
		}

		/// <summary>
		/// See <see cref="DefinitionSchemaDigest"/>
		/// </summary>
		public static readonly BindableProperty DefinitionSchemaDigestProperty =
			BindableProperty.Create(nameof(DefinitionSchemaDigest), typeof(byte[]), typeof(TokenDetailsViewModel), default(byte[]));

		/// <summary>
		/// Digest of schema used to validate token definition XML.
		/// </summary>
		public byte[] DefinitionSchemaDigest
		{
			get => (byte[])this.GetValue(DefinitionSchemaDigestProperty);
			set => this.SetValue(DefinitionSchemaDigestProperty, value);
		}

		/// <summary>
		/// See <see cref="DefinitionSchemaHashFunction"/>
		/// </summary>
		public static readonly BindableProperty DefinitionSchemaHashFunctionProperty =
			BindableProperty.Create(nameof(DefinitionSchemaHashFunction), typeof(HashFunction), typeof(TokenDetailsViewModel), default(HashFunction));

		/// <summary>
		/// Hash function used to compute <see cref="DefinitionSchemaDigest"/>.
		/// </summary>
		public HashFunction DefinitionSchemaHashFunction
		{
			get => (HashFunction)this.GetValue(DefinitionSchemaHashFunctionProperty);
			set => this.SetValue(DefinitionSchemaHashFunctionProperty, value);
		}

		/// <summary>
		/// See <see cref="CreatorCanDestroy"/>
		/// </summary>
		public static readonly BindableProperty CreatorCanDestroyProperty =
			BindableProperty.Create(nameof(CreatorCanDestroy), typeof(bool), typeof(TokenDetailsViewModel), default(bool));

		/// <summary>
		/// If the creator can destroy the token.
		/// </summary>
		public bool CreatorCanDestroy
		{
			get => (bool)this.GetValue(CreatorCanDestroyProperty);
			set => this.SetValue(CreatorCanDestroyProperty, value);
		}

		/// <summary>
		/// See <see cref="OwnerCanDestroyBatch"/>
		/// </summary>
		public static readonly BindableProperty OwnerCanDestroyBatchProperty =
			BindableProperty.Create(nameof(OwnerCanDestroyBatch), typeof(bool), typeof(TokenDetailsViewModel), default(bool));

		/// <summary>
		/// If the owner can destroy the entire batch of tokens, if owner of every token in the batch.
		/// </summary>
		public bool OwnerCanDestroyBatch
		{
			get => (bool)this.GetValue(OwnerCanDestroyBatchProperty);
			set => this.SetValue(OwnerCanDestroyBatchProperty, value);
		}

		/// <summary>
		/// See <see cref="OwnerCanDestroyIndividual"/>
		/// </summary>
		public static readonly BindableProperty OwnerCanDestroyIndividualProperty =
			BindableProperty.Create(nameof(OwnerCanDestroyIndividual), typeof(bool), typeof(TokenDetailsViewModel), default(bool));

		/// <summary>
		/// If the owner can destroy an individual token.
		/// </summary>
		public bool OwnerCanDestroyIndividual
		{
			get => (bool)this.GetValue(OwnerCanDestroyIndividualProperty);
			set => this.SetValue(OwnerCanDestroyIndividualProperty, value);
		}

		/// <summary>
		/// See <see cref="CertifierCanDestroy"/>
		/// </summary>
		public static readonly BindableProperty CertifierCanDestroyProperty =
			BindableProperty.Create(nameof(CertifierCanDestroy), typeof(bool), typeof(TokenDetailsViewModel), default(bool));

		/// <summary>
		/// If a certifier can destroy the token.
		/// </summary>
		public bool CertifierCanDestroy
		{
			get => (bool)this.GetValue(CertifierCanDestroyProperty);
			set => this.SetValue(CertifierCanDestroyProperty, value);
		}

		/// <summary>
		/// See <see cref="FriendlyName"/>
		/// </summary>
		public static readonly BindableProperty FriendlyNameProperty =
			BindableProperty.Create(nameof(FriendlyName), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Friendly name of token.
		/// </summary>
		public string FriendlyName
		{
			get => (string)this.GetValue(FriendlyNameProperty);
			set => this.SetValue(FriendlyNameProperty, value);
		}

		/// <summary>
		/// See <see cref="GlyphContentType"/>
		/// </summary>
		public static readonly BindableProperty GlyphContentTypeProperty =
			BindableProperty.Create(nameof(GlyphContentType), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Content-Type of glyph
		/// </summary>
		public string GlyphContentType
		{
			get => (string)this.GetValue(GlyphContentTypeProperty);
			set => this.SetValue(GlyphContentTypeProperty, value);
		}

		/// <summary>
		/// See <see cref="Ordinal"/>
		/// </summary>
		public static readonly BindableProperty OrdinalProperty =
			BindableProperty.Create(nameof(Ordinal), typeof(int), typeof(TokenDetailsViewModel), default(int));

		/// <summary>
		/// Ordinal of token, within batch.
		/// </summary>
		public int Ordinal
		{
			get => (int)this.GetValue(OrdinalProperty);
			set => this.SetValue(OrdinalProperty, value);
		}

		/// <summary>
		/// See <see cref="Value"/>
		/// </summary>
		public static readonly BindableProperty ValueProperty =
			BindableProperty.Create(nameof(Value), typeof(decimal), typeof(TokenDetailsViewModel), default(decimal));

		/// <summary>
		/// (Last) Value of token
		/// </summary>
		public decimal Value
		{
			get => (decimal)this.GetValue(ValueProperty);
			set => this.SetValue(ValueProperty, value);
		}

		/// <summary>
		/// See <see cref="Witness"/>
		/// </summary>
		public static readonly BindableProperty WitnessProperty =
			BindableProperty.Create(nameof(Witness), typeof(string[]), typeof(TokenDetailsViewModel), default(string[]));

		/// <summary>
		/// Witnesses
		/// </summary>
		public string[] Witness
		{
			get => (string[])this.GetValue(WitnessProperty);
			set => this.SetValue(WitnessProperty, value);
		}

		/// <summary>
		/// See <see cref="CertifierJids"/>
		/// </summary>
		public static readonly BindableProperty CertifierJidsProperty =
			BindableProperty.Create(nameof(CertifierJids), typeof(string[]), typeof(TokenDetailsViewModel), default(string[]));

		/// <summary>
		/// JIDs of certifiers
		/// </summary>
		public string[] CertifierJids
		{
			get => (string[])this.GetValue(CertifierJidsProperty);
			set => this.SetValue(CertifierJidsProperty, value);
		}

		/// <summary>
		/// See <see cref="Certifier"/>
		/// </summary>
		public static readonly BindableProperty CertifierProperty =
			BindableProperty.Create(nameof(Certifier), typeof(string[]), typeof(TokenDetailsViewModel), default(string[]));

		/// <summary>
		/// Certifiers
		/// </summary>
		public string[] Certifier
		{
			get => (string[])this.GetValue(CertifierProperty);
			set => this.SetValue(CertifierProperty, value);
		}

		/// <summary>
		/// See <see cref="TokenIdMethod"/>
		/// </summary>
		public static readonly BindableProperty TokenIdMethodProperty =
			BindableProperty.Create(nameof(TokenIdMethod), typeof(TokenIdMethod), typeof(TokenDetailsViewModel), default(TokenIdMethod));

		/// <summary>
		/// Method of assigning the Token ID.
		/// </summary>
		public TokenIdMethod TokenIdMethod
		{
			get => (TokenIdMethod)this.GetValue(TokenIdMethodProperty);
			set => this.SetValue(TokenIdMethodProperty, value);
		}

		/// <summary>
		/// See <see cref="TokenId"/>
		/// </summary>
		public static readonly BindableProperty TokenIdProperty =
			BindableProperty.Create(nameof(TokenId), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Token ID
		/// </summary>
		public string TokenId
		{
			get => (string)this.GetValue(TokenIdProperty);
			set => this.SetValue(TokenIdProperty, value);
		}

		/// <summary>
		/// See <see cref="Visibility"/>
		/// </summary>
		public static readonly BindableProperty VisibilityProperty =
			BindableProperty.Create(nameof(Visibility), typeof(ContractVisibility), typeof(TokenDetailsViewModel), default(ContractVisibility));

		/// <summary>
		/// Visibility of token
		/// </summary>
		public ContractVisibility Visibility
		{
			get => (ContractVisibility)this.GetValue(VisibilityProperty);
			set => this.SetValue(VisibilityProperty, value);
		}

		/// <summary>
		/// See <see cref="Creator"/>
		/// </summary>
		public static readonly BindableProperty CreatorProperty =
			BindableProperty.Create(nameof(Creator), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Creator of token
		/// </summary>
		public string Creator
		{
			get => (string)this.GetValue(CreatorProperty);
			set => this.SetValue(CreatorProperty, value);
		}

		/// <summary>
		/// See <see cref="CreatorJid"/>
		/// </summary>
		public static readonly BindableProperty CreatorJidProperty =
			BindableProperty.Create(nameof(CreatorJid), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// JID of <see cref="Creator"/>.
		/// </summary>
		public string CreatorJid
		{
			get => (string)this.GetValue(CreatorJidProperty);
			set => this.SetValue(CreatorJidProperty, value);
		}

		/// <summary>
		/// See <see cref="Owner"/>
		/// </summary>
		public static readonly BindableProperty OwnerProperty =
			BindableProperty.Create(nameof(Owner), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Current owner
		/// </summary>
		public string Owner
		{
			get => (string)this.GetValue(OwnerProperty);
			set => this.SetValue(OwnerProperty, value);
		}

		/// <summary>
		/// See <see cref="BatchSize"/>
		/// </summary>
		public static readonly BindableProperty BatchSizeProperty =
			BindableProperty.Create(nameof(BatchSize), typeof(int), typeof(TokenDetailsViewModel), default(int));

		/// <summary>
		/// Number of tokens in batch being created.
		/// </summary>
		public int BatchSize
		{
			get => (int)this.GetValue(BatchSizeProperty);
			set => this.SetValue(BatchSizeProperty, value);
		}

		/// <summary>
		/// See <see cref="TrustProvider"/>
		/// </summary>
		public static readonly BindableProperty TrustProviderProperty =
			BindableProperty.Create(nameof(TrustProvider), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Trust Provider asserting the validity of the token
		/// </summary>
		public string TrustProvider
		{
			get => (string)this.GetValue(TrustProviderProperty);
			set => this.SetValue(TrustProviderProperty, value);
		}

		/// <summary>
		/// See <see cref="OwnerJid"/>
		/// </summary>
		public static readonly BindableProperty OwnerJidProperty =
			BindableProperty.Create(nameof(OwnerJid), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// JID of owner
		/// </summary>
		public string OwnerJid
		{
			get => (string)this.GetValue(OwnerJidProperty);
			set => this.SetValue(OwnerJidProperty, value);
		}

		/// <summary>
		/// See <see cref="Currency"/>
		/// </summary>
		public static readonly BindableProperty CurrencyProperty =
			BindableProperty.Create(nameof(Currency), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Currency of <see cref="Value"/>.
		/// </summary>
		public string Currency
		{
			get => (string)this.GetValue(CurrencyProperty);
			set => this.SetValue(CurrencyProperty, value);
		}

		/// <summary>
		/// See <see cref="Reference"/>
		/// </summary>
		public static readonly BindableProperty ReferenceProperty =
			BindableProperty.Create(nameof(Reference), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Any reference provided by the token creator.
		/// </summary>
		public string Reference
		{
			get => (string)this.GetValue(ReferenceProperty);
			set => this.SetValue(ReferenceProperty, value);
		}

		/// <summary>
		/// See <see cref="Definition"/>
		/// </summary>
		public static readonly BindableProperty DefinitionProperty =
			BindableProperty.Create(nameof(Definition), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// XML Definition of token.
		/// </summary>
		public string Definition
		{
			get => (string)this.GetValue(DefinitionProperty);
			set => this.SetValue(DefinitionProperty, value);
		}

		/// <summary>
		/// See <see cref="DefinitionNamespace"/>
		/// </summary>
		public static readonly BindableProperty DefinitionNamespaceProperty =
			BindableProperty.Create(nameof(DefinitionNamespace), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// XML Namespace used in the <see cref="Definition"/>
		/// </summary>
		public string DefinitionNamespace
		{
			get => (string)this.GetValue(DefinitionNamespaceProperty);
			set => this.SetValue(DefinitionNamespaceProperty, value);
		}

		/// <summary>
		/// See <see cref="CreationContract"/>
		/// </summary>
		public static readonly BindableProperty CreationContractProperty =
			BindableProperty.Create(nameof(CreationContract), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Contract used to create the contract.
		/// </summary>
		public string CreationContract
		{
			get => (string)this.GetValue(CreationContractProperty);
			set => this.SetValue(CreationContractProperty, value);
		}

		/// <summary>
		/// See <see cref="OwnershipContract"/>
		/// </summary>
		public static readonly BindableProperty OwnershipContractProperty =
			BindableProperty.Create(nameof(OwnershipContract), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Contract used to define the current ownership
		/// </summary>
		public string OwnershipContract
		{
			get => (string)this.GetValue(OwnershipContractProperty);
			set => this.SetValue(OwnershipContractProperty, value);
		}

		/// <summary>
		/// See <see cref="Valuator"/>
		/// </summary>
		public static readonly BindableProperty ValuatorProperty =
			BindableProperty.Create(nameof(Valuator), typeof(string[]), typeof(TokenDetailsViewModel), default(string[]));

		/// <summary>
		/// Valuators
		/// </summary>
		public string[] Valuator
		{
			get => (string[])this.GetValue(ValuatorProperty);
			set => this.SetValue(ValuatorProperty, value);
		}

		/// <summary>
		/// See <see cref="Assessor"/>
		/// </summary>
		public static readonly BindableProperty AssessorProperty =
			BindableProperty.Create(nameof(Assessor), typeof(string[]), typeof(TokenDetailsViewModel), default(string[]));

		/// <summary>
		/// Assessors
		/// </summary>
		public string[] Assessor
		{
			get => (string[])this.GetValue(AssessorProperty);
			set => this.SetValue(AssessorProperty, value);
		}

		/// <summary>
		/// See <see cref="TrustProviderJid"/>
		/// </summary>
		public static readonly BindableProperty TrustProviderJidProperty =
			BindableProperty.Create(nameof(TrustProviderJid), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// JID of <see cref="TrustProvider"/>
		/// </summary>
		public string TrustProviderJid
		{
			get => (string)this.GetValue(TrustProviderJidProperty);
			set => this.SetValue(TrustProviderJidProperty, value);
		}

		/// <summary>
		/// See <see cref="Tags"/>
		/// </summary>
		public static readonly BindableProperty TagsProperty =
			BindableProperty.Create(nameof(Tags), typeof(TokenTag[]), typeof(TokenDetailsViewModel), default(TokenTag[]));

		/// <summary>
		/// Any custom Token Tags provided during creation of the token.
		/// </summary>
		public TokenTag[] Tags
		{
			get => (TokenTag[])this.GetValue(TagsProperty);
			set => this.SetValue(TagsProperty, value);
		}

		/// <summary>
		/// See <see cref="GlyphImage"/>
		/// </summary>
		public static readonly BindableProperty GlyphImageProperty =
			BindableProperty.Create(nameof(GlyphImage), typeof(ImageSource), typeof(TokenDetailsViewModel), default(ImageSource));

		/// <summary>
		/// Gets or sets the image representing the glyph.
		/// </summary>
		public ImageSource GlyphImage
		{
			get => (ImageSource)this.GetValue(GlyphImageProperty);
			set => this.SetValue(GlyphImageProperty, value);
		}

		/// <summary>
		/// See <see cref="HasGlyphImage"/>
		/// </summary>
		public static readonly BindableProperty HasGlyphImageProperty =
			BindableProperty.Create(nameof(HasGlyphImage), typeof(bool), typeof(TokenDetailsViewModel), default(bool));

		/// <summary>
		/// Gets or sets the value representing of a glyph is available or not.
		/// </summary>
		public bool HasGlyphImage
		{
			get => (bool)this.GetValue(HasGlyphImageProperty);
			set => this.SetValue(HasGlyphImageProperty, value);
		}

		/// <summary>
		/// See <see cref="GlyphWidth"/>
		/// </summary>
		public static readonly BindableProperty GlyphWidthProperty =
			BindableProperty.Create(nameof(GlyphWidth), typeof(int), typeof(TokenDetailsViewModel), default(int));

		/// <summary>
		/// Gets or sets the value representing the width of the glyph.
		/// </summary>
		public int GlyphWidth
		{
			get => (int)this.GetValue(GlyphWidthProperty);
			set => this.SetValue(GlyphWidthProperty, value);
		}

		/// <summary>
		/// See <see cref="GlyphHeight"/>
		/// </summary>
		public static readonly BindableProperty GlyphHeightProperty =
			BindableProperty.Create(nameof(GlyphHeight), typeof(int), typeof(TokenDetailsViewModel), default(int));

		/// <summary>
		/// Gets or sets the value representing the height of the glyph.
		/// </summary>
		public int GlyphHeight
		{
			get => (int)this.GetValue(GlyphHeightProperty);
			set => this.SetValue(GlyphHeightProperty, value);
		}

		#endregion

	}
}