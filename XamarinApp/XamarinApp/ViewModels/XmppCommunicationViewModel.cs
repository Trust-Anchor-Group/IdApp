using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Xamarin.Forms;

namespace XamarinApp.ViewModels
{
    public class XmppCommunicationViewModel : BaseViewModel
    {
        private readonly INeuronService neuronService;

        public XmppCommunicationViewModel()
        {
            this.neuronService = DependencyService.Resolve<INeuronService>();
            this.RefreshCommand = new Command(_ => GetHtmlContent());
        }

        public ICommand RefreshCommand { get; }

        protected override async Task DoBind()
        {
            await base.DoBind();
            GetHtmlContent();
        }

        protected override async Task DoUnbind()
        {
            await base.DoUnbind();
        }

        public static readonly BindableProperty HtmlProperty =
            BindableProperty.Create("Html", typeof(HtmlWebViewSource), typeof(XmppCommunicationViewModel), default(HtmlWebViewSource));

        public HtmlWebViewSource Html
        {
            get { return (HtmlWebViewSource) GetValue(HtmlProperty); }
            set { SetValue(HtmlProperty, value); }
        }

        public void GetHtmlContent()
        {
            this.Html = new HtmlWebViewSource
            {
                Html = this.neuronService.CommsDumpAsHtml()
            };
        }
    }
}