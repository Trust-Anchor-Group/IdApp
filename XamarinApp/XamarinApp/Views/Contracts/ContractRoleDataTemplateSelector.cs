using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace XamarinApp.Views.Contracts
{
    public class ContractRoleDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Role1 { get; set; }
        public DataTemplate Role2 { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Role role = (Role)item;
            return Role1;
        }
    }
}