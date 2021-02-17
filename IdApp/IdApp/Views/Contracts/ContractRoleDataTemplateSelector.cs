using IdApp.Models;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A data template selector for displaying various types of contract roles.
    /// </summary>
    public class ContractRoleDataTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// The first role template.
        /// </summary>
        public DataTemplate Role1 { get; set; }
        /// <summary>
        /// The second role template.
        /// </summary>
        public DataTemplate Role2 { get; set; }

        /// <summary>
        /// Chooses the best matching data template based on the type of contract role.
        /// </summary>
        /// <param name="item">The role to display.</param>
        /// <param name="container"></param>
        /// <returns></returns>
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            RoleModel model = (RoleModel)item;
            return Role1;
        }
    }
}