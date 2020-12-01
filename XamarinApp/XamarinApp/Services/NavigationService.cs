using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Runtime.Queue;
using Xamarin.Forms;

namespace XamarinApp.Services
{
    internal sealed class NavigationService : INavigationService
    {
        private readonly AsyncQueue<MessageRecord> messageQueue;
        private bool isDisplayingMessages;

        public NavigationService()
        {
            this.messageQueue = new AsyncQueue<MessageRecord>();
            this.isDisplayingMessages = false;
        }

        public Task PushAsync(Page page)
        {
            return Application.Current.MainPage.Navigation.PushAsync(page, true);
        }

        public Task<Page> PopAsync()
        {
            return Application.Current.MainPage.Navigation.PopAsync(true);
        }

        public Task PushModalAsync(Page page)
        {
            return Application.Current.MainPage.Navigation.PushModalAsync(page, true);
        }

        public Task PopModalAsync()
        {
            return Application.Current.MainPage.Navigation.PopModalAsync(true);
        }

        public async Task ReplaceAsync(Page page)
        {
            // Neat trick to replace current page but still get a page animation.
            Page currPage = Application.Current.MainPage;
            if (currPage is NavigationPage navPage)
            {
                currPage = navPage.CurrentPage;
            }
            await Application.Current.MainPage.Navigation.PushAsync(page);
            currPage.Navigation.RemovePage(currPage);
        }

        public async Task DisplayAlert(string title, string message, string accept, string cancel)
        {
            this.messageQueue.Add(new MessageRecord(title, message, accept, cancel));

            if (!this.isDisplayingMessages)
                await Device.InvokeOnMainThreadAsync(DisplayMessages);
        }

        public async Task DisplayAlert(string title, string message, string accept)
        {
            this.messageQueue.Add(new MessageRecord(title, message, accept, null));

            if (!this.isDisplayingMessages)
                await Device.InvokeOnMainThreadAsync(DisplayMessages);
        }

        public async Task DisplayAlert(string title, string message)
        {
            this.messageQueue.Add(new MessageRecord(title, message, null, null));

            if (!this.isDisplayingMessages)
                await Device.InvokeOnMainThreadAsync(DisplayMessages);
        }

        public async Task DisplayAlert(string title, string message, Exception exception)
        {
            StringBuilder sb = new StringBuilder();
            if (exception != null)
            {
                if (exception is AggregateException aggregate && aggregate.InnerException != null)
                {
                    exception = aggregate.InnerException;
                }

                sb.AppendLine(exception.Message);
                while ((exception = exception.InnerException) != null)
                {
                    sb.AppendLine(exception.Message);
                }
            }
            else
            {
                sb.AppendLine(AppResources.AnErrorHasOccurred);
            }

            await this.DisplayAlert(title ?? AppResources.ErrorTitle, sb.ToString(), AppResources.Ok);
        }

        public Task DisplayAlert(string title, Exception exception)
        {
            return this.DisplayAlert(title, null, exception);
        }

        public Task DisplayAlert(Exception exception)
        {
            IAppInformation appInfo = DependencyService.Get<IAppInformation>();
            string title = $"{AppResources.ErrorTitle} (v{appInfo.GetVersion()})";
            return this.DisplayAlert(title, null, exception);
        }

        private async Task DisplayMessages()
        {
            this.isDisplayingMessages = true;
            try
            {
                do
                {
                    MessageRecord record = await this.messageQueue.Wait();
                    if (string.IsNullOrWhiteSpace(record.Accept) && !string.IsNullOrWhiteSpace(record.Cancel))
                    {
                        await Application.Current.MainPage.DisplayAlert(record.Title, record.Message, record.Cancel);
                    }
                    else if (!string.IsNullOrWhiteSpace(record.Accept) && string.IsNullOrWhiteSpace(record.Cancel))
                    {
                        await Application.Current.MainPage.DisplayAlert(record.Title, record.Message, record.Accept);
                    }
                    else if (string.IsNullOrWhiteSpace(record.Accept) && string.IsNullOrWhiteSpace(record.Cancel))
                    {
                        await Application.Current.MainPage.DisplayAlert(record.Title, record.Message, AppResources.Ok);
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(record.Title, record.Message, record.Accept, record.Cancel);
                    }
                }
                while (this.messageQueue.CountItems > 0);
            }
            finally
            {
                this.isDisplayingMessages = false;
            }
        }

        private class MessageRecord
        {
            public MessageRecord(string title, string message, string accept, string cancel)
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
        }
    }
}