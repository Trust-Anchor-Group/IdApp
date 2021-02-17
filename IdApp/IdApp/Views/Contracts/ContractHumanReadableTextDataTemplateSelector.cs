using Waher.Networking.XMPP.Contracts.HumanReadable;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
{
    /// <summary>
    /// A data template selector for displaying various types of human readable text.
    /// </summary>
    public class ContractHumanReadableTextDataTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// The first human readable text template.
        /// </summary>
        public DataTemplate HumanReadableText1 { get; set; }
        /// <summary>
        /// The second human readable text template.
        /// </summary>
        public DataTemplate HumanReadableText2 { get; set; }

        /// <summary>
        /// Chooses the best matching data template based on the type of human readable text.
        /// </summary>
        /// <param name="item">The human readable text to display.</param>
        /// <param name="container"></param>
        /// <returns></returns>
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            HumanReadableText text = (HumanReadableText)item;
            return HumanReadableText1;
        }
    }
}