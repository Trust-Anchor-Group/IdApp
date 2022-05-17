using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using System.Xml;
using IdApp.DeviceSpecific;
using IdApp.Extensions;
using IdApp.Pages.Contacts;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Contacts.MyContacts;
using IdApp.Pages.Contracts.NewContract;
using IdApp.Pages.Wallet.TokenEvents;
using IdApp.Resx;
using IdApp.Services;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using NeuroFeatures.Events;
using NeuroFeatures.Tags;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Security;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of a token.
	/// </summary>
	public class TokenDetailsViewModel : QrXmppViewModel
	{
		private readonly TokenDetailsPage page;

		/// <summary>
		/// Creates an instance of the <see cref="TokenDetailsViewModel"/> class.
		/// </summary>
		/// <param name="Page">Page hosting details.</param>
		public TokenDetailsViewModel(TokenDetailsPage Page)
			: base()
		{
			this.page = Page;

			this.Certifiers = new();
			this.Valuators = new();
			this.Assessors = new();
			this.Witnesses = new();
			this.Tags = new();

			this.CopyToClipboardCommand = new Command(async P => await this.CopyToClipboard(P));
			this.ViewIdCommand = new Command(async P => await this.ViewId(P));
			this.ViewContractCommand = new Command(async P => await this.ViewContract(P));
			this.OpenChatCommand = new Command(async P => await this.OpenChat(P));
			this.OpenLinkCommand = new Command(async P => await this.OpenLink(P));
			this.ShowDetailsCommand = new Command(async _ => await this.ShowDetails());
			this.SendToContactCommand = new Command(async _ => await this.SendToContact());
			this.ShareCommand = new Command(async _ => await this.Share());
			this.OfferToSellCommand = new Command(async _ => await this.OfferToSell());
			this.OfferToBuyCommand = new Command(async _ => await this.OfferToBuy());
			this.ViewEventsCommand = new Command(async _ => await this.ViewEvents());
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
				this.TokenIdMethod = args.Token.TokenIdMethod;
				this.TokenId = args.Token.TokenId;
				this.Visibility = args.Token.Visibility;
				this.Creator = args.Token.Creator;
				this.CreatorFriendlyName = await ContactInfo.GetFriendlyName(args.Token.Creator, this);
				this.CreatorJid = args.Token.CreatorJid;
				this.Owner = args.Token.Owner;
				this.OwnerFriendlyName = await ContactInfo.GetFriendlyName(args.Token.Owner, this);
				this.OwnerJid = args.Token.OwnerJid;
				this.BatchSize = args.Token.BatchSize;
				this.TrustProvider = args.Token.TrustProvider;
				this.TrustProviderFriendlyName = await ContactInfo.GetFriendlyName(args.Token.TrustProvider, this);
				this.TrustProviderJid = args.Token.TrustProviderJid;
				this.Currency = args.Token.Currency;
				this.Reference = args.Token.Reference;
				this.Definition = args.Token.Definition;
				this.DefinitionNamespace = args.Token.DefinitionNamespace;
				this.CreationContract = args.Token.CreationContract;
				this.OwnershipContract = args.Token.OwnershipContract;
				this.GlyphImage = args.Token.GlyphImage;
				this.HasGlyphImage = args.Token.HasGlyphImage;
				this.GlyphWidth = args.Token.GlyphWidth;
				this.GlyphHeight = args.Token.GlyphHeight;
				this.TokenXml = args.Token.Token.ToXml();
				this.IsMyToken = string.Compare(this.OwnerJid, this.XmppService.BareJid, true) == 0;

				if (!string.IsNullOrEmpty(args.Token.Reference))
				{
					if (Uri.TryCreate(args.Token.Reference, UriKind.Absolute, out Uri RefUri) &&
						RefUri.Scheme.ToLower() is string s &&
						(s == "http" || s == "https"))
					{
						this.page.AddLink(this, AppResources.Reference, s);   // TODO: Replace with grouped collection, when this works in Xamarin.
					}
				}

				if (!(args.Token.Tags is null))
				{
					foreach (TokenTag Tag in args.Token.Tags)
						this.page.AddLink(this, Tag.Name, Tag.Value?.ToString() ?? string.Empty);   // TODO: Replace with grouped collection, when this works in Xamarin.
				}

				this.GenerateQrCode(Constants.UriSchemes.CreateTokenUri(this.TokenId));

				await this.Populate(AppResources.Witness, string.Empty, args.Token.Witness, null, this.Witnesses);
				await this.Populate(AppResources.Certifier, AppResources.CertifierJid, args.Token.Certifier, args.Token.CertifierJids, this.Certifiers);
				await this.Populate(AppResources.Valuator, string.Empty, args.Token.Valuator, null, this.Valuators);
				await this.Populate(AppResources.Assessor, string.Empty, args.Token.Assessor, null, this.Assessors);

				this.Tags.Clear();

				if (!(args.Token.Tags is null))
				{
					foreach (TokenTag Tag in args.Token.Tags)
						this.Tags.Add(Tag);
				}

				StringBuilder sb = new();
				string Domain = this.TokenId.After("@");
				Domain = Domain.After("."); // Remove last sub-domain, corresponding to the component hosting the token.

				sb.Append("https://");
				sb.Append(Domain);
				sb.Append("/ValidationSchema.md?NS=");
				sb.Append(HttpUtility.UrlEncode(this.DefinitionNamespace));
				sb.Append("&H=");
				sb.Append(HttpUtility.UrlEncode(Convert.ToBase64String(this.DefinitionSchemaDigest)));
				sb.Append("&Download=1");

				this.DefinitionSchemaUrl = sb.ToString();	// TODO: The above assume contract hosted by the TAG Neuron. URL should be retrieved using API, or be standardized.
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

		private async Task Populate(string LegalIdLabel, string JidLabel, string[] LegalIds, string[] Jids, ObservableCollection<PartItem> Parts)
		{
			int i, c = LegalIds.Length;
			int d = Jids?.Length ?? 0;
			string FriendlyName;
			string Jid;

			Parts.Clear();

			for (i = 0; i < c; i++)
			{
				FriendlyName = await ContactInfo.GetFriendlyName(LegalIds[i], this);
				Jid = i < d ? Jids[i] : string.Empty;

				Parts.Add(new PartItem(LegalIds[i], Jid, FriendlyName));

				this.page.AddLegalId(this, LegalIdLabel, FriendlyName, LegalIds[i]);    // TODO: Replace with grouped collection, when this works in Xamarin.

				if (!string.IsNullOrEmpty(Jid))
					this.page.AddJid(this, JidLabel, Jid, LegalIds[i], FriendlyName);   // TODO: Replace with grouped collection, when this works in Xamarin.
			}
		}

		/// <summary>
		/// See <see cref="DefinitionSchemaUrl"/>
		/// </summary>
		public static readonly BindableProperty DefinitionSchemaUrlProperty =
			BindableProperty.Create(nameof(DefinitionSchemaUrl), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Token ID
		/// </summary>
		public string DefinitionSchemaUrl
		{
			get => (string)this.GetValue(DefinitionSchemaUrlProperty);
			set => this.SetValue(DefinitionSchemaUrlProperty, value);
		}

		#region Properties

		/// <summary>
		/// Certifiers
		/// </summary>
		public ObservableCollection<PartItem> Certifiers { get; }

		/// <summary>
		/// Valuators
		/// </summary>
		public ObservableCollection<PartItem> Valuators { get; }

		/// <summary>
		/// Assessors
		/// </summary>
		public ObservableCollection<PartItem> Assessors { get; }

		/// <summary>
		/// Witnesses
		/// </summary>
		public ObservableCollection<PartItem> Witnesses { get; }

		/// <summary>
		/// Witnesses
		/// </summary>
		public ObservableCollection<TokenTag> Tags { get; }

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
		/// See <see cref="TokenXml"/>
		/// </summary>
		public static readonly BindableProperty TokenXmlProperty =
			BindableProperty.Create(nameof(TokenXml), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Token ID
		/// </summary>
		public string TokenXml
		{
			get => (string)this.GetValue(TokenXmlProperty);
			set => this.SetValue(TokenXmlProperty, value);
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
		/// See <see cref="CreatorFriendlyName"/>
		/// </summary>
		public static readonly BindableProperty CreatorFriendlyNameProperty =
			BindableProperty.Create(nameof(CreatorFriendlyName), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// CreatorFriendlyName of token
		/// </summary>
		public string CreatorFriendlyName
		{
			get => (string)this.GetValue(CreatorFriendlyNameProperty);
			set => this.SetValue(CreatorFriendlyNameProperty, value);
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
		/// See <see cref="OwnerFriendlyName"/>
		/// </summary>
		public static readonly BindableProperty OwnerFriendlyNameProperty =
			BindableProperty.Create(nameof(OwnerFriendlyName), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Current owner
		/// </summary>
		public string OwnerFriendlyName
		{
			get => (string)this.GetValue(OwnerFriendlyNameProperty);
			set => this.SetValue(OwnerFriendlyNameProperty, value);
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
		/// See <see cref="TrustProviderFriendlyName"/>
		/// </summary>
		public static readonly BindableProperty TrustProviderFriendlyNameProperty =
			BindableProperty.Create(nameof(TrustProviderFriendlyName), typeof(string), typeof(TokenDetailsViewModel), default(string));

		/// <summary>
		/// Trust Provider asserting the validity of the token
		/// </summary>
		public string TrustProviderFriendlyName
		{
			get => (string)this.GetValue(TrustProviderFriendlyNameProperty);
			set => this.SetValue(TrustProviderFriendlyNameProperty, value);
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

		/// <summary>
		/// See <see cref="IsMyToken"/>
		/// </summary>
		public static readonly BindableProperty IsMyTokenProperty =
			BindableProperty.Create(nameof(IsMyToken), typeof(bool), typeof(TokenDetailsViewModel), default(bool));

		/// <summary>
		/// If the token belongs to the user.
		/// </summary>
		public bool IsMyToken
		{
			get => (bool)this.GetValue(IsMyTokenProperty);
			set => this.SetValue(IsMyTokenProperty, value);
		}

		#endregion

		#region Commands

		/// <summary>
		/// Command to copy a value to the clipboard.
		/// </summary>
		public ICommand CopyToClipboardCommand { get; }

		/// <summary>
		/// Command to view a Legal ID.
		/// </summary>
		public ICommand ViewIdCommand { get; }

		/// <summary>
		/// Command to view a smart contract.
		/// </summary>
		public ICommand ViewContractCommand { get; }

		/// <summary>
		/// Command to open a chat page.
		/// </summary>
		public ICommand OpenChatCommand { get; }

		/// <summary>
		/// Command to open a link.
		/// </summary>
		public ICommand OpenLinkCommand { get; }

		/// <summary>
		/// Command to show machine-readable details of token.
		/// </summary>
		public ICommand ShowDetailsCommand { get; }

		/// <summary>
		/// Command to send token to contact
		/// </summary>
		public ICommand SendToContactCommand { get; }

		/// <summary>
		/// Command to share token with other applications
		/// </summary>
		public ICommand ShareCommand { get; }

		/// <summary>
		/// Command to offer the token for sale.
		/// </summary>
		public ICommand OfferToSellCommand { get; }

		/// <summary>
		/// Command to offer to buy the token.
		/// </summary>
		public ICommand OfferToBuyCommand { get; }

		/// <summary>
		/// Command to view events related to token.
		/// </summary>
		public ICommand ViewEventsCommand { get; }

		private async Task CopyToClipboard(object Parameter)
		{
			try
			{
				string s = Parameter?.ToString() ?? string.Empty;
				int i = s.IndexOf('@');

				if (i > 0 && Guid.TryParse(s.Substring(0, i), out _))
				{
					await Clipboard.SetTextAsync(Constants.UriSchemes.UriSchemeNeuroFeature + ":" + s);
					await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.IdCopiedSuccessfully);
				}
				else
				{
					await Clipboard.SetTextAsync(s);
					await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.TagValueCopiedToClipboard);
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ViewId(object Parameter)
		{
			try
			{
				await this.ContractOrchestratorService.OpenLegalIdentity(Parameter.ToString(), AppResources.PurposeReviewToken);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ViewContract(object Parameter)
		{
			try
			{
				await this.ContractOrchestratorService.OpenContract(Parameter.ToString(), AppResources.PurposeReviewToken);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task OpenChat(object Parameter)
		{
			string s = Parameter.ToString();
			string BareJid;
			string LegalId;
			string FriendlyName;

			switch (s)
			{
				case "Owner":
					BareJid = this.OwnerJid;
					LegalId = this.Owner;
					FriendlyName = this.OwnerFriendlyName;
					break;

				case "Creator":
					BareJid = this.CreatorJid;
					LegalId = this.Creator;
					FriendlyName = this.CreatorFriendlyName;
					break;

				case "TrustProvider":
					BareJid = this.TrustProviderJid;
					LegalId = this.TrustProvider;
					FriendlyName = this.TrustProviderFriendlyName;
					break;

				default:
					string[] Parts = s.Split(" | ");
					if (Parts.Length != 3)
						return;

					BareJid = Parts[0];
					LegalId = Parts[1];
					FriendlyName = Parts[2];
					break;
			}

			try
			{
				await this.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(LegalId, BareJid, FriendlyName));
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private Task OpenLink(object Parameter)
		{
			return App.OpenUrl(Parameter.ToString());
		}

		private async Task ShowDetails()
		{
			try
			{
				byte[] Bin = Encoding.UTF8.GetBytes(this.Definition);
				HttpFileUploadEventArgs e = await this.XmppService.FileUploadClient.RequestUploadSlotAsync(this.TokenId + ".xml", "text/xml; charset=utf-8", Bin.Length);

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

		private async Task SendToContact()
		{
			TaskCompletionSource<ContactInfo> Selected = new();
			ContactListNavigationArgs Args = new(AppResources.SendInformationTo, Selected)
			{
				CanScanQrCode = true,
				CancelReturnCounter = true
			};

			await this.NavigationService.GoToAsync(nameof(MyContactsPage), Args);

			ContactInfo Contact = await Args.Selection.Task;
			if (Contact is null)
				return;

			StringBuilder Markdown = new();

			Markdown.Append("```");
			Markdown.AppendLine(Constants.UriSchemes.UriSchemeNeuroFeature);
			Markdown.AppendLine(this.TokenXml);
			Markdown.AppendLine("```");

			await ChatViewModel.ExecuteSendMessage(string.Empty, Markdown.ToString(), Contact.BareJid, this);

			await Task.Delay(100);	// Otherwise, page doesn't show properly. (Underlying timing issue. TODO: Find better solution.)

			ChatNavigationArgs Args2 = new(Contact);
			await this.NavigationService.GoToAsync(nameof(ChatPage), Args2);
		}

		private async Task Share()
		{
			try
			{
				if (this.QrCodeBin is null)
					return;

				IShareContent shareContent = DependencyService.Get<IShareContent>();
				string FileName = "Token.QR." + InternetContent.GetFileExtension(this.QrCodeContentType);

				shareContent.ShareImage(this.QrCodeBin, this.FriendlyName, AppResources.Share, FileName);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task OfferToSell()
		{
			try
			{
				Contract Template = await this.XmppService.Contracts.GetContract(Constants.ContractTemplates.TransferTokenTemplate);
				Template.Visibility = ContractVisibility.Public;

				NewContractNavigationArgs NewContractArgs = new(Template, true);

				if (Template.ForMachinesLocalName == "Transfer" && Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures)
				{
					CreationAttributesEventArgs e = await this.XmppService.Wallet.GetCreationAttributes();
					XmlDocument Doc = new();
					Doc.LoadXml(Template.ForMachines.OuterXml);

					NewContractArgs.SuppressProposal(e.TrustProviderId);

					XmlNamespaceManager NamespaceManager = new(Doc.NameTable);
					NamespaceManager.AddNamespace("nft", NeuroFeaturesClient.NamespaceNeuroFeatures);

					string SellerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Seller/nft:RoleReference/@role", NamespaceManager)?.Value;
					string TrustProviderRole = Doc.SelectSingleNode("/nft:Transfer/nft:TrustProvider/nft:RoleReference/@role", NamespaceManager)?.Value;
					string TokenIdParameter = Doc.SelectSingleNode("/nft:Transfer/nft:TokenID/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string CurrencyParameter = Doc.SelectSingleNode("/nft:Transfer/nft:Currency/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string CommissionParameter = Doc.SelectSingleNode("/nft:Transfer/nft:CommissionPercent/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string OwnershipContractParameter = Doc.SelectSingleNode("/nft:Transfer/nft:OwnershipContract/nft:ParameterReference/@parameter", NamespaceManager)?.Value;

					if (Template.Parts is null)
					{
						List<Part> Parts = new();

						if (!string.IsNullOrEmpty(SellerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = this.TagProfile.LegalIdentity.Id,
								Role = SellerRole
							});
						}

						if (!string.IsNullOrEmpty(TrustProviderRole))
						{
							Parts.Add(new Part()
							{
								LegalId = e.TrustProviderId,
								Role = TrustProviderRole
							});
						}

						Template.Parts = Parts.ToArray();
						Template.PartsMode = ContractParts.ExplicitlyDefined;
					}
					else
					{
						foreach (Part Part in Template.Parts)
						{
							if (Part.Role == SellerRole)
								Part.LegalId = this.TagProfile.LegalIdentity.Id;
							else if (Part.Role == TrustProviderRole)
								Part.LegalId = e.TrustProviderId;
						}
					}

					if (!string.IsNullOrEmpty(TokenIdParameter))
						Template[TokenIdParameter] = this.TokenId;

					if (!string.IsNullOrEmpty(CurrencyParameter))
						Template[CurrencyParameter] = e.Currency;

					if (!string.IsNullOrEmpty(CommissionParameter))
						Template[CommissionParameter] = e.Commission;

					if (!string.IsNullOrEmpty(OwnershipContractParameter))
						Template[OwnershipContractParameter] = this.OwnershipContract;
				}

				await this.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task OfferToBuy()
		{
			try
			{
				Contract Template = await this.XmppService.Contracts.GetContract(Constants.ContractTemplates.TransferTokenTemplate);
				Template.Visibility = ContractVisibility.Public;

				NewContractNavigationArgs NewContractArgs = new(Template, true);

				if (Template.ForMachinesLocalName == "Transfer" && Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures)
				{
					CreationAttributesEventArgs e = await this.XmppService.Wallet.GetCreationAttributes();
					XmlDocument Doc = new();
					Doc.LoadXml(Template.ForMachines.OuterXml);

					NewContractArgs.SuppressProposal(e.TrustProviderId);

					XmlNamespaceManager NamespaceManager = new(Doc.NameTable);
					NamespaceManager.AddNamespace("nft", NeuroFeaturesClient.NamespaceNeuroFeatures);

					string BuyerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Buyer/nft:RoleReference/@role", NamespaceManager)?.Value;
					string SellerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Seller/nft:RoleReference/@role", NamespaceManager)?.Value;
					string TrustProviderRole = Doc.SelectSingleNode("/nft:Transfer/nft:TrustProvider/nft:RoleReference/@role", NamespaceManager)?.Value;
					string TokenIdParameter = Doc.SelectSingleNode("/nft:Transfer/nft:TokenID/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string CurrencyParameter = Doc.SelectSingleNode("/nft:Transfer/nft:Currency/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string CommissionParameter = Doc.SelectSingleNode("/nft:Transfer/nft:CommissionPercent/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string OwnershipContractParameter = Doc.SelectSingleNode("/nft:Transfer/nft:OwnershipContract/nft:ParameterReference/@parameter", NamespaceManager)?.Value;

					if (Template.Parts is null)
					{
						List<Part> Parts = new();

						if (!string.IsNullOrEmpty(BuyerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = this.TagProfile.LegalIdentity.Id,
								Role = BuyerRole
							});
						}

						if (!string.IsNullOrEmpty(SellerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = this.Owner,
								Role = SellerRole
							});
						}

						if (!string.IsNullOrEmpty(TrustProviderRole))
						{
							Parts.Add(new Part()
							{
								LegalId = e.TrustProviderId,
								Role = TrustProviderRole
							});
						}

						Template.Parts = Parts.ToArray();
						Template.PartsMode = ContractParts.ExplicitlyDefined;
					}
					else
					{
						foreach (Part Part in Template.Parts)
						{
							if (Part.Role == BuyerRole)
								Part.LegalId = this.TagProfile.LegalIdentity.Id;
							else if (Part.Role == SellerRole)
								Part.LegalId = this.Owner;
							else if (Part.Role == TrustProviderRole)
								Part.LegalId = e.TrustProviderId;
						}
					}

					if (!string.IsNullOrEmpty(TokenIdParameter))
						Template[TokenIdParameter] = this.TokenId;

					if (!string.IsNullOrEmpty(CurrencyParameter))
						Template[CurrencyParameter] = e.Currency;

					if (!string.IsNullOrEmpty(CommissionParameter))
						Template[CommissionParameter] = e.Commission;

					if (!string.IsNullOrEmpty(OwnershipContractParameter))
						Template[OwnershipContractParameter] = this.OwnershipContract;
				}

				await this.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ViewEvents()
		{
			try
			{
				TokenEvent[] Events = await this.XmppService.Wallet.NeuroFeaturesClient.GetEventsAsync(this.TokenId);

				await this.NavigationService.GoToAsync(nameof(TokenEventsPage),
					new TokenEventsNavigationArgs(this.TokenId, Events) { CancelReturnCounter = true });
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		#endregion

	}
}