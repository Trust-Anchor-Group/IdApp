using Waher.Networking.XMPP.Contracts.HumanReadable;
using Xamarin.Forms;

namespace IdApp.Views.Contracts
{
    public class ContractHumanReadableTextDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HumanReadableText1 { get; set; }
        public DataTemplate HumanReadableText2 { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            HumanReadableText text = (HumanReadableText)item;
            return HumanReadableText1;
        }
    }
}