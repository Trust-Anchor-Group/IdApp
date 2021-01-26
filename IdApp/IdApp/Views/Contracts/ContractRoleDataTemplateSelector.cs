using IdApp.Models;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
{
    public class ContractRoleDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Role1 { get; set; }
        public DataTemplate Role2 { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            RoleModel model = (RoleModel)item;
            return Role1;
        }
    }
}