using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Xamarin.Forms;

namespace IdApp.ViewModels
{
    /// <summary>
    /// A View model that represents the current XMPP Communication.
    /// </summary>
    public class XmppCommunicationViewModel : BaseViewModel
    {
        private readonly INeuronService neuronService;

        /// <summary>
        /// Create a new instance of an <see cref="XmppCommunicationViewModel"/>.
        /// </summary>
        public XmppCommunicationViewModel()
            : this(null)
        {
        }

        /// <summary>
        /// Create a new instance of an <see cref="XmppCommunicationViewModel"/>.
        /// For unit tests.
        /// </summary>
        protected internal XmppCommunicationViewModel(INeuronService neuronService)
        {
            this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            this.RefreshCommand = new Command(_ => GetHtmlContent());
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();
            GetHtmlContent();
        }

        /// <inheritdoc/>
        protected override async Task DoUnbind()
        {
            await base.DoUnbind();
        }

        /// <summary>
        /// The command for executing a Refresh action.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty HtmlProperty =
            BindableProperty.Create("Html", typeof(HtmlWebViewSource), typeof(XmppCommunicationViewModel), default(HtmlWebViewSource));

        /// <summary>
        /// The html code to display
        /// </summary>
        public HtmlWebViewSource Html
        {
            get { return (HtmlWebViewSource) GetValue(HtmlProperty); }
            set { SetValue(HtmlProperty, value); }
        }

        /// <summary>
        /// Retrieves and assigns the <see cref="Html"/>.
        /// </summary>
        public void GetHtmlContent()
        {
            this.Html = new HtmlWebViewSource
            {
                Html = this.neuronService.CommsDumpAsHtml()
            };
        }
    }
}