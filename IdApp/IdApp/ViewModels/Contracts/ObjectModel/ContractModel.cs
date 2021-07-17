using System;
using System.Globalization;
using System.Text;
using Tag.Neuron.Xamarin.Extensions;
using Waher.Content.Markdown;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Networking.XMPP.Contracts.HumanReadable.BlockElements;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements;

namespace IdApp.ViewModels.Contracts.ObjectModel
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

        /// <summary>
        /// Creates an instance of the <see cref="ContractModel"/> class.
        /// </summary>
        /// <param name="ContractId">The contract id.</param>
        /// <param name="Timestamp">The timestamp to show with the contract reference.</param>
        /// <param name="Contract">Contract</param>
        public ContractModel(string ContractId, DateTime Timestamp, Contract Contract)
        {
            this.contract = Contract;
            this.contractId = ContractId;
            this.timestamp = Timestamp.ToString(CultureInfo.CurrentUICulture);
            this.category = GetCategory(Contract) ?? Contract.ForMachinesNamespace + "#" + Contract.ForMachinesLocalName;
            this.name = Contract.ContractId;
        }

        private static string GetCategory(Contract Contract)
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
                        StringBuilder Markdown = new StringBuilder();

                        foreach (InlineElement Item in Section.Header)
                            Item.GenerateMarkdown(Markdown, 1, Contract);

                        MarkdownDocument Doc = new MarkdownDocument(Markdown.ToString());

                        return Doc.GeneratePlainText().Trim();
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