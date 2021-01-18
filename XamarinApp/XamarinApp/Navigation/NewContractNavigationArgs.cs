using System.Collections.Generic;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Navigation
{
    public class NewContractNavigationArgs : NavigationArgs
    {
        public NewContractNavigationArgs(Contract contract)
        {
            this.Contract = contract;
        }

        public NewContractNavigationArgs(SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
        {
            this.Contract = null;
            this.ContractTypesPerCategory = contractTypesPerCategory;
        }

        public NewContractNavigationArgs(Contract contract, SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
        {
            this.Contract = contract;
            this.ContractTypesPerCategory = contractTypesPerCategory;
        }

        public Contract Contract { get; }

        public SortedDictionary<string, SortedDictionary<string, string>> ContractTypesPerCategory { get; }
    }
}