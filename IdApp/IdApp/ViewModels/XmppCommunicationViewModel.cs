using System.Threading;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Collections.Generic;

namespace IdApp.ViewModels
{
    /// <summary>
    /// A View model that represents the current XMPP Communication.
    /// </summary>
    public class XmppCommunicationViewModel : BaseViewModel
    {
        private readonly INeuronService neuronService;
        private readonly ILogService logService;

        private CancellationTokenSource tokenSource;

        /// <summary>
        /// Time between checks for debug update in milliseconds.
        /// </summary>
        private int autoUpdateTimeInMilliseconds = 8000;
        /// <summary>
        /// 
        /// </summary>
        public static readonly BindableProperty HistoryButtonTextProperty =
           BindableProperty.Create("HistoryButtonText", typeof(string), typeof(string), default(string));
        /// <summary>
        /// History/Actual button lable.
        /// </summary>
        public string HistoryButtonText
        {
            get { return (string)GetValue(HistoryButtonTextProperty); }
            set { SetValue(HistoryButtonTextProperty, value); }
        }

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

            logService = DependencyService.Resolve<ILogService>();
            ClearCommand = new Command(_ => ClearHtmlContent());
            CopyCommand = new Command(_ => CopyHtmlToClipboard());
            ShowHistoryCommand = new Command(_ => showHTMLHistory());

            HistoryButtonText = "History";

            runAutoUpdateTask();
        }

        /// <inheritdoc/>
        protected override async Task DoBind()
        {
            await base.DoBind();
        }

        /// <inheritdoc/>
        protected override async Task DoUnbind()
        {
            tokenSource.Cancel();
            await base.DoUnbind();
        }

        /// <summary>
        /// The command for clearing inspect window.
        /// </summary>
        public ICommand ClearCommand { get; }
        /// <summary>
        /// The command for copying data to clipboard.
        /// </summary>
        public ICommand CopyCommand { get; }
        /// <summary>
        /// The command that toggle to show history or actual data.
        /// </summary>
        public ICommand ShowHistoryCommand { get; }

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
        /// Copies debug info to clipboard
        /// </summary>
        public void CopyHtmlToClipboard()
        {
            Clipboard.SetTextAsync(neuronService.CommsDumpAsText(HistoryButtonText));
        }
        /// <summary>
        /// Clears actual data (History data is not changed).
        /// </summary>
        public void ClearHtmlContent()
        {
            if(HistoryButtonText == "History")
            {
                Html.Html = string.Empty;
                neuronService.ClearHtmlContent();
            }
        }
        /// <summary>
        /// Toggles showing history or actual debug info.
        /// </summary>
        private void showHTMLHistory()
        {
            if (HistoryButtonText == "History")
            {
                HistoryButtonText = "Actual";
                tokenSource.Cancel();
                Html.Html = neuronService.CommsDumpAsHtml(true);
            } else
            {
                HistoryButtonText = "History";
                runAutoUpdateTask();
            }            
        }
        /// <summary>
        /// Create cancellationtoken and run debug auto update task.
        /// </summary>
        private void runAutoUpdateTask()
        {
            tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            Task.Run(async () => { await AutoRefresh(); }, token);
        }

        /// <summary>
        /// Retrieves and assigns the <see cref="Html"/>.
        /// </summary>
        private async Task AutoRefresh()
        {
            try
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    string newHtml = neuronService.CommsDumpAsHtml();

                    if (Html == null || !Html.Html.GetHashCode().Equals(newHtml.GetHashCode()))
                    {
                        Html = new HtmlWebViewSource
                        {
                            Html = newHtml
                        };
                    }

                    await Task.Delay(autoUpdateTimeInMilliseconds);
                }
            }
            catch(Exception ex)
            {
                this.logService.LogException(ex, new KeyValuePair<string, string>("Method", "AutoDebugInspect"));
            }
              
        }
    }
}