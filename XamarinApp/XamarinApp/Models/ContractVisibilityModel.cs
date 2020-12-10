using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.Models
{
    public class ContractVisibilityModel
    {
        public ContractVisibilityModel(ContractVisibility visibility, string name)
        {
            this.Visibility = visibility;
            this.Name = name;
        }

        public ContractVisibility Visibility { get; }
        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}