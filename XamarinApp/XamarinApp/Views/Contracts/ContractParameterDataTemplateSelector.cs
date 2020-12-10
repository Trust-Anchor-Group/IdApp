using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace XamarinApp.Views.Contracts
{
    public class ContractParameterDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Parameter1 { get; set; }
        public DataTemplate Parameter2 { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Parameter parameter = (Parameter)item;
            return Parameter1;
        }
    }
}