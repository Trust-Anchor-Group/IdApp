using IdApp.Models;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A data template selector for displaying various types of contract parameters.
    /// </summary>
    public class ContractParameterDataTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// The first parameter template.
        /// </summary>
        public DataTemplate Parameter1 { get; set; }
        /// <summary>
        /// The second parameter template.
        /// </summary>
        public DataTemplate Parameter2 { get; set; }

        /// <summary>
        /// Chooses the best matching data template based on the type of contract parameter.
        /// </summary>
        /// <param name="item">The parameter to display.</param>
        /// <param name="container"></param>
        /// <returns></returns>
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            ParameterModel model = (ParameterModel)item;
            return Parameter1;
        }
    }
}