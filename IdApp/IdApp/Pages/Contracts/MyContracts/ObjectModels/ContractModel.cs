using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdApp.Extensions;
using IdApp.Services;
using IdApp.Services.Notification;
using Waher.Content.Markdown;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Networking.XMPP.Contracts.HumanReadable.BlockElements;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements;

namespace IdApp.Pages.Contracts.MyContracts.ObjectModels
{
    /// <summary>
    /// The data model for a contract.
    /// </summary>
    public class ContractModel : IItemGroup
	{
        private readonly string contractId;
        private readonly string category;
        private readonly string name;
        private readonly DateTime timestamp;
        private readonly Contract contract;
		private readonly NotificationEvent[] events;


		private ContractModel(string ContractId, DateTime Timestamp, Contract Contract, string Category, string Name, NotificationEvent[] Events)
        {
            this.contract = Contract;
            this.contractId = ContractId;
            this.timestamp = Timestamp;
            this.category = Category;
            this.name = Name;
			this.events = Events;
        }

        /// <summary>
        /// Creates an instance of the <see cref="ContractModel"/> class.
        /// </summary>
        /// <param name="ContractId">The contract id.</param>
        /// <param name="Timestamp">The timestamp to show with the contract reference.</param>
        /// <param name="Contract">Contract</param>
        /// <param name="Ref">Service References</param>
		/// <param name="Events">Notification events associated with contract.</param>
        public static async Task<ContractModel> Create(string ContractId, DateTime Timestamp, Contract Contract, IServiceReferences Ref,
			NotificationEvent[] Events)
        {
            string Category = await GetCategory(Contract) ?? Contract.ForMachinesNamespace + "#" + Contract.ForMachinesLocalName;
            string Name = await GetName(Contract, Ref) ?? Contract.ContractId;

            return new ContractModel(ContractId, Timestamp, Contract, Category, Name, Events);
        }

        /// <summary>
        /// Gets a displayable name for a contract.
        /// </summary>
        /// <param name="Contract">Contract</param>
        /// <param name="Ref">Service References</param>
        /// <returns>Displayable Name</returns>
        public static async Task<string> GetName(Contract Contract, IServiceReferences Ref)
		{
            if (Contract.Parts is null)
                return null;

            Dictionary<string, Waher.Networking.XMPP.Contracts.ClientSignature> Signatures = new();
            StringBuilder sb = null;

            if (Contract.ClientSignatures is not null)
            {
                foreach (Waher.Networking.XMPP.Contracts.ClientSignature Signature in Contract.ClientSignatures)
                    Signatures[Signature.LegalId] = Signature;
            }

            foreach (Part Part in Contract.Parts)
			{
                if (Part.LegalId == Ref.TagProfile.LegalJid ||
                    (Signatures.TryGetValue(Part.LegalId, out Waher.Networking.XMPP.Contracts.ClientSignature PartSignature) &&
                    string.Compare(PartSignature.BareJid, Ref.XmppService.BareJid, true) == 0))
                {
                    continue;   // Self
                }

                string FriendlyName = await ContactInfo.GetFriendlyName(Part.LegalId, Ref);

                if (sb is null)
                    sb = new StringBuilder(FriendlyName);
                else
				{
                    sb.Append(", ");
                    sb.Append(FriendlyName);
				}
			}

            return sb?.ToString();
        }

        /// <summary>
        /// Gets the category of a contract
        /// </summary>
        /// <param name="Contract">Contract</param>
        /// <returns>Contract Category</returns>
        public static async Task<string> GetCategory(Contract Contract)
        {
            HumanReadableText[] Localizations = Contract.ForHumans;
            string Language = Contract.DeviceLanguage();

            foreach (HumanReadableText Localization in Localizations)
			{
                if (string.Compare(Localization.Language, Language, true) != 0)
                    continue;

                foreach (BlockElement Block in Localization.Body)
                {
                    if (Block is Section Section)
                    {
                        StringBuilder Markdown = new();

                        foreach (InlineElement Item in Section.Header)
                            Item.GenerateMarkdown(Markdown, 1, new Waher.Networking.XMPP.Contracts.HumanReadable.MarkdownSettings(Contract, MarkdownType.ForRendering));

                        MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown.ToString());

                        return (await Doc.GeneratePlainText()).Trim();
                    }
                }
			}

            return null;
		}

        /// <summary>
        /// The contract id.
        /// </summary>
        public string ContractId => this.contractId;

		/// <inheritdoc/>
		public string UniqueName => this.ContractId;

		/// <summary>
		/// The created timestamp of the contract.
		/// </summary>
		public DateTime Timestamp => this.timestamp;

        /// <summary>
        /// A reference to the contract.
        /// </summary>
        public Contract Contract => this.contract;

        /// <summary>
        /// Displayable name for the contract.
        /// </summary>
        public string Name => this.name;

		/// <summary>
		/// Name, or category if no name.
		/// </summary>
		public string NameOrCategory => string.IsNullOrEmpty(this.name) ? this.category : this.name;

        /// <summary>
        /// Displayable category for the contract.
        /// </summary>
        public string Category => this.category;

		/// <summary>
		/// If the contract has associated notification events.
		/// </summary>
		public bool HasEvents => this.NrEvents > 0;

		/// <summary>
		/// Number of notification events associated with the contract.
		/// </summary>
		public int NrEvents => this.events?.Length ?? 0;

		/// <summary>
		/// Notification events.
		/// </summary>
		public NotificationEvent[] Events => this.events;
	}
}
