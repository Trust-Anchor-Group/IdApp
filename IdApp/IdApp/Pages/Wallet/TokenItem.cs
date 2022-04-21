using NeuroFeatures;
using NeuroFeatures.Tags;
using System;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Security;

namespace IdApp.Pages.Wallet
{
	/// <summary>
	/// Encapsulates a <see cref="Token"/> object.
	/// </summary>
	public class TokenItem
	{
		private readonly Token token;

		/// <summary>
		/// Encapsulates a <see cref="Token"/> object.
		/// </summary>
		/// <param name="Token">Token</param>
		public TokenItem(Token Token)
		{
			this.token = Token;
		}

		/// <summary>
		/// When token was created.
		/// </summary>
		public DateTime Created => this.token.Created;

		/// <summary>
		/// When token was last updated.
		/// </summary>
		public DateTime Updated => this.token.Updated;

		/// <summary>
		/// When token expires.
		/// </summary>
		public DateTime Expires => this.token.Expires;

		/// <summary>
		/// Required archiving time after token expires.
		/// </summary>
		public Duration? ArchiveRequired => this.token.ArchiveRequired;

		/// <summary>
		/// Optional archiving time after required archiving time.
		/// </summary>
		public Duration? ArchiveOptional => this.token.ArchiveOptional;

		/// <summary>
		/// Signature timestamp
		/// </summary>
		public DateTime SignatureTimestamp => this.token.SignatureTimestamp;

		/// <summary>
		/// Token signature
		/// </summary>
		public byte[] Signature => this.token.Signature;

		/// <summary>
		/// Digest of schema used to validate token definition XML.
		/// </summary>
		public byte[] DefinitionSchemaDigest => this.token.DefinitionSchemaDigest;

		/// <summary>
		/// Hash function used to compute <see cref="DefinitionSchemaDigest"/>.
		/// </summary>
		public HashFunction DefinitionSchemaHashFunction => this.token.DefinitionSchemaHashFunction;

		/// <summary>
		/// If the creator can destroy the token.
		/// </summary>
		public bool CreatorCanDestroy => this.token.CreatorCanDestroy;

		/// <summary>
		/// If the owner can destroy the entire batch of tokens, if owner of every token in the batch.
		/// </summary>
		public bool OwnerCanDestroyBatch => this.token.OwnerCanDestroyBatch;

		/// <summary>
		/// If the owner can destroy an individual token.
		/// </summary>
		public bool OwnerCanDestroyIndividual => this.token.OwnerCanDestroyIndividual;

		/// <summary>
		/// If a certifier can destroy the token.
		/// </summary>
		public bool CertifierCanDestroy => this.token.CertifierCanDestroy;

		/// <summary>
		/// Friendly name of token.
		/// </summary>
		public string FriendlyName => this.token.FriendlyName;

		/// <summary>
		/// Glyph of token.
		/// </summary>
		public byte[] Glyph => this.token.Glyph;

		/// <summary>
		/// Content-Type of glyph
		/// </summary>
		public string GlyphContentType => this.token.GlyphContentType;

		/// <summary>
		/// Width of glyph
		/// </summary>
		public int GlyphWidth => this.token.GlyphWidth;

		/// <summary>
		/// Height of glyph
		/// </summary>
		public int GlyphHeight => this.token.GlyphHeight;

		/// <summary>
		/// Ordinal of token, within batch.
		/// </summary>
		public int Ordinal => this.token.Ordinal;

		/// <summary>
		/// (Last) Value of token
		/// </summary>
		public decimal Value => this.token.Value;

		/// <summary>
		/// Witnesses
		/// </summary>
		public string[] Witness => this.token.Witness;

		/// <summary>
		/// JIDs of certifiers
		/// </summary>
		public string[] CertifierJids => this.token.CertifierJids;

		/// <summary>
		/// Certifiers
		/// </summary>
		public string[] Certifier => this.token.Certifier;

		/// <summary>
		/// Method of assigning the Token ID.
		/// </summary>
		public TokenIdMethod TokenIdMethod => this.token.TokenIdMethod;

		/// <summary>
		/// Token ID
		/// </summary>
		public string TokenId => this.token.TokenId;

		/// <summary>
		/// Visibility of token
		/// </summary>
		public ContractVisibility Visibility => this.token.Visibility;

		/// <summary>
		/// Creator of token
		/// </summary>
		public string Creator => this.token.Creator;

		/// <summary>
		/// JID of <see cref="Creator"/>.
		/// </summary>
		public string CreatorJid => this.token.CreatorJid;

		/// <summary>
		/// Current owner
		/// </summary>
		public string Owner => this.token.Owner;

		/// <summary>
		/// Number of tokens in batch being created.
		/// </summary>
		public int BatchSize => this.token.BatchSize;

		/// <summary>
		/// Trust Provider asserting the validity of the token
		/// </summary>
		public string TrustProvider => this.token.TrustProvider;

		/// <summary>
		/// JID of owner
		/// </summary>
		public string OwnerJid => this.token.OwnerJid;

		/// <summary>
		/// Currency of <see cref="Value"/>.
		/// </summary>
		public string Currency => this.token.Currency;

		/// <summary>
		/// Any reference provided by the token creator.
		/// </summary>
		public string Reference => this.token.Reference;

		/// <summary>
		/// XML Definition of token.
		/// </summary>
		public string Definition => this.token.Definition;

		/// <summary>
		/// XML Namespace used in the <see cref="Definition"/>
		/// </summary>
		public string DefinitionNamespace => this.token.DefinitionNamespace;

		/// <summary>
		/// Contract used to create the contract.
		/// </summary>
		public string CreationContract => this.token.CreationContract;

		/// <summary>
		/// Contract used to define the current ownership
		/// </summary>
		public string OwnershipContract => this.token.OwnershipContract;

		/// <summary>
		/// Valuators
		/// </summary>
		public string[] Valuator => this.token.Valuator;

		/// <summary>
		/// Assessors
		/// </summary>
		public string[] Assessor => this.token.Assessor;

		/// <summary>
		/// JID of <see cref="TrustProvider"/>
		/// </summary>
		public string TrustProviderJid => this.token.TrustProviderJid;

		/// <summary>
		/// Any custom Token Tags provided during creation of the token.
		/// </summary>
		public TokenTag[] Tags => this.token.Tags;
	}
}
