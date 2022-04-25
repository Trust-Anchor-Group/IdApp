using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using IdApp.Extensions;
using IdApp.Services;
using IdApp.Services.Xmpp;
using IdApp.Services.Tag;
using Waher.Content.Markdown;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Networking.XMPP.Contracts.HumanReadable.BlockElements;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements;
using IdApp.Services.Contracts;

namespace IdApp.Pages.Contracts.MyContracts.ObjectModel
{
    /// <summary>
    /// The data model for a contract.
    /// </summary>
    public class ContractModel
    {
        private readonly string contractId;
        private readonly string timestamp;
        private readonly string category;
        private readonly string name;
        private readonly Contract contract;

        private ContractModel(string ContractId, DateTime Timestamp, Contract Contract, string Category, string Name)
        {
            this.contract = Contract;
            this.contractId = ContractId;
            this.timestamp = Timestamp.ToString(CultureInfo.CurrentUICulture);
            this.category = Category;
            this.name = Name;
        }

        /// <summary>
        /// Creates an instance of the <see cref="ContractModel"/> class.
        /// </summary>
        /// <param name="ContractId">The contract id.</param>
        /// <param name="Timestamp">The timestamp to show with the contract reference.</param>
        /// <param name="Contract">Contract</param>
        /// <param name="TagProfile">TAG profile</param>
        /// <param name="XmppService">XMPP service.</param>
        /// <param name="SmartContracts">Smart Contracts interface.</param>
        public static async Task<ContractModel> Create(string ContractId, DateTime Timestamp, Contract Contract, ITagProfile TagProfile,
            IXmppService XmppService, ISmartContracts SmartContracts)
        {
            string Category = await GetCategory(Contract) ?? Contract.ForMachinesNamespace + "#" + Contract.ForMachinesLocalName;
            string Name = await GetName(Contract, TagProfile, XmppService, SmartContracts) ?? Contract.ContractId;

            return new ContractModel(ContractId, Timestamp, Contract, Category, Name);
        }

        /// <summary>
        /// Gets a displayable name for a contract.
        /// </summary>
        /// <param name="Contract">Contract</param>
        /// <param name="TagProfile">TAG Profile</param>
        /// <param name="XmppService">XMPP Service</param>
        /// <param name="SmartContracts">Smart Contracts interface.</param>
        /// <returns>Displayable Name</returns>
        public static async Task<string> GetName(Contract Contract, ITagProfile TagProfile, IXmppService XmppService, 
            ISmartContracts SmartContracts)
		{
            if (Contract.Parts is null)
                return null;

            Dictionary<string, Waher.Networking.XMPP.Contracts.ClientSignature> Signatures = new();
            StringBuilder sb = null;

            if (!(Contract.ClientSignatures is null))
            {
                foreach (Waher.Networking.XMPP.Contracts.ClientSignature Signature in Contract.ClientSignatures)
                    Signatures[Signature.LegalId] = Signature;
            }

            foreach (Part Part in Contract.Parts)
			{
                if (Part.LegalId == TagProfile.LegalJid ||
                    (Signatures.TryGetValue(Part.LegalId, out Waher.Networking.XMPP.Contracts.ClientSignature PartSignature) &&
                    string.Compare(PartSignature.BareJid, XmppService.BareJid, true) == 0))
                {
                    continue;   // Self
                }

                string FriendlyName = await ContactInfo.GetFriendlyName(Part.LegalId, XmppService.Xmpp, TagProfile, SmartContracts);

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

        /// <summary>
        /// The created timestamp of the contract.
        /// </summary>
        public string Timestamp => this.timestamp;

        /// <summary>
        /// A reference to the contract.
        /// </summary>
        public Contract Contract => this.contract;

        /// <summary>
        /// Displayable name for the contract.
        /// </summary>
        public string Name => this.name;

        /// <summary>
        /// Displayable category for the contract.
        /// </summary>
        public string Category => this.category;
    }
}