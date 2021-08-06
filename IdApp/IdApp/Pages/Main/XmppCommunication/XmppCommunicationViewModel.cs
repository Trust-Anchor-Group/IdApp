using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Main.XmppCommunication
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
        private const int AutoUpdateTimeInMilliseconds = 8000;

        /// <summary>
        /// Create a new instance of an <see cref="XmppCommunicationViewModel"/>.
        /// </summary>
        public XmppCommunicationViewModel()
            : this(null, null)
        {
        }

        /// <summary>
        /// Create a new instance of an <see cref="XmppCommunicationViewModel"/>.
        /// For unit tests.
        /// </summary>
        protected internal XmppCommunicationViewModel(INeuronService neuronService, ILogService logService)
        {
            this.neuronService = neuronService ?? App.Instantiate<INeuronService>();
            this.logService = logService ?? App.Instantiate<ILogService>();

            ClearCommand = new Command(_ => this.ClearHtmlContent());
            CopyCommand = new Command(_ => this.CopyHtmlToClipboard());
            ShowHistoryCommand = new Command(_ => this.ShowHtmlHistory());
            SendDebugInfoCommand = new Command(_ => this.SendDebugInfo());

            HistoryButtonText = "History";

            RunAutoUpdateTask();
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
        /// The command for sending files to server for debug.
        /// </summary>
        public ICommand SendDebugInfoCommand { get; }

        /// <summary>
        /// <see cref="HistoryButtonText"/>
        /// </summary>
        public static readonly BindableProperty HistoryButtonTextProperty =

            BindableProperty.Create("HistoryButtonText", typeof(string), typeof(string), default(string));
        /// <summary>
        /// History/Actual button label.
        /// </summary>
        public string HistoryButtonText
        {
            get { return (string)GetValue(HistoryButtonTextProperty); }
            set { SetValue(HistoryButtonTextProperty, value); }
        }

        /// <summary>
        /// <see cref="Html"/>
        /// </summary>
        public static readonly BindableProperty HtmlProperty =
            BindableProperty.Create("Html", typeof(HtmlWebViewSource), typeof(XmppCommunicationViewModel), default(HtmlWebViewSource));

        /// <summary>
        /// The html code to display
        /// </summary>
        public HtmlWebViewSource Html
        {
            get { return (HtmlWebViewSource)GetValue(HtmlProperty); }
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
            if (HistoryButtonText == "History")
            {
                Html.Html = string.Empty;
                neuronService.ClearHtmlContent();
            }
        }
        /// <summary>
        /// Toggles showing history or actual debug info.
        /// </summary>
        private void ShowHtmlHistory()
        {
            if (HistoryButtonText == "History")
            {
                HistoryButtonText = "Actual";
                tokenSource.Cancel();
                Html.Html = neuronService.CommsDumpAsHtml(true);
            }
            else
            {
                HistoryButtonText = "History";
                RunAutoUpdateTask();
            }
        }
        /// <summary>
        /// Creates a CancellationToken and runs the debug auto update task.
        /// </summary>
        private void RunAutoUpdateTask()
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

                    if (Html is null || !Html.Html.GetHashCode().Equals(newHtml.GetHashCode()))
                    {
                        Html = new HtmlWebViewSource
                        {
                            Html = newHtml
                        };
                    }

                    await Task.Delay(AutoUpdateTimeInMilliseconds);
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
            }

        }

        private async void SendDebugInfo()
        {
            bool answer = await Application.Current.MainPage.DisplayAlert("Send Debug Information", "Are you sure you want to send debug information to TAG? This will include key information, so you should create a new ID once troubleshooting is completed.", "Yes", "No");

            if (answer)
            {
                try
                {
                    var configPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var filesList = Directory.GetFiles(configPath);
                    string contentType;
                    string message;
                    string extension;

                    foreach (var file in filesList)
                    {
                        extension = Path.GetExtension(file);

                        if (extension == ".uml")
                            continue;

                        contentType = Waher.Content.InternetContent.GetContentType(extension);
                        message = File.ReadAllText(file);

                        await App.SendAlert(message, contentType);
                    }

                    foreach (var file in filesList)
                    {
                        extension = Path.GetExtension(file);

                        if (extension != ".uml")
                            continue;

                        contentType = "text/markdown";
                        message = $"```uml\n{File.ReadAllText(file)}```";

                        await App.SendAlert(message, contentType);
                    }
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                }
            }
        }
    }
}