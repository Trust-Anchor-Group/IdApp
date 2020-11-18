using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Xamarin.Forms;
using Waher.Runtime.Queue;

namespace XamarinApp.Services
{
    internal sealed class MessageService : IMessageService
    {
        private readonly AsyncQueue<MessageRec> messageQueue;
        private bool isDisplayingMessages;

        public MessageService()
        {
            this.messageQueue = new AsyncQueue<MessageRec>();
            this.isDisplayingMessages = false;
        }

        public async Task DisplayAlert(string title, string message, string accept, string cancel)
        {
            messageQueue.Add(new MessageRec(title, message, accept, cancel));

            if (!isDisplayingMessages)
                await Device.InvokeOnMainThreadAsync(DisplayMessages);
        }

        public async Task DisplayAlert(string title, string message, string accept)
        {
            messageQueue.Add(new MessageRec(title, message, accept, null));

            if (!isDisplayingMessages)
                await Device.InvokeOnMainThreadAsync(DisplayMessages);
        }

        public async Task DisplayAlert(Exception exception)
        {
            exception = Log.UnnestException(exception);

            if (exception is AggregateException aggregateException)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception innerException in aggregateException.InnerExceptions)
                    sb.AppendLine(innerException.Message);
                await DisplayAlert(AppResources.ErrorTitle, sb.ToString(), AppResources.Ok);
            }
            else
            {
                await DisplayAlert(AppResources.ErrorTitle, exception.Message, AppResources.Ok);
            }
        }

        private async Task DisplayMessages()
        {
            isDisplayingMessages = true;
            try
            {
                do
                {
                    MessageRec rec = await messageQueue.Wait();
                    if (string.IsNullOrWhiteSpace(rec.Accept))
                    {
                        await App.Instance.MainPage.DisplayAlert(rec.Title, rec.Message, rec.Cancel);
                    }
                    else if(string.IsNullOrWhiteSpace(rec.Cancel))
                    {
                        await App.Instance.MainPage.DisplayAlert(rec.Title, rec.Message, rec.Accept);
                    }
                    else
                    {
                        await App.Instance.MainPage.DisplayAlert(rec.Title, rec.Message, rec.Accept, rec.Cancel);
                    }
                }
                while (messageQueue.CountItems > 0);
            }
            finally
            {
                isDisplayingMessages = false;
            }
        }

        private class MessageRec
        {
            public MessageRec(string title, string message, string accept, string cancel)
            {
                Title = title;
                Message = message;
                Accept = accept;
                Cancel = cancel;
            }

            public string Title { get; }
            public string Message { get; }
            public string Accept { get; }
            public string Cancel { get; }
            public bool Prompt { get; set; }
        }
    }
}
