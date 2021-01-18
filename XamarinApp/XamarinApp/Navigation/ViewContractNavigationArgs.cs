using System.Collections.Generic;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Navigation
{
    public class ViewContractNavigationArgs : NavigationArgs
    {
        public ViewContractNavigationArgs(Contract contract, bool isReadOnly)
        {
            this.Contract = contract;
            this.IsReadOnly = isReadOnly;
        }

        public Contract Contract { get; }

        public bool IsReadOnly { get; }
    }
}