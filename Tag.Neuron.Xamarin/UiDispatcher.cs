﻿using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin
{
    /// <inheritdoc/>
    [Singleton]
    public class UiDispatcher : IUiDispatcher
    {
        private readonly ConcurrentQueue<MessageRecord> messageQueue;
        private bool isDisplayingMessages;

        /// <summary>
        /// Creates a new instance of the <see cref="UiDispatcher"/> class.
        /// </summary>
        public UiDispatcher()
        {
            this.messageQueue = new ConcurrentQueue<MessageRecord>();
            this.isDisplayingMessages = false;
            this.IsRunningInTheBackground = false;
        }

        /// <inheritdoc/>
        public void BeginInvokeOnMainThread(Action action)
        {
            Device.BeginInvokeOnMainThread(action);
        }

        /// <inheritdoc/>
        public bool IsRunningInTheBackground { get; set; }

        private void StartDisplay()
        {
            if (!this.isDisplayingMessages)
            {
                this.isDisplayingMessages = true;
                this.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAllMessages();
                });
            }
        }

        /// <inheritdoc/>
        public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            MessageRecord record = new MessageRecord(title, message, accept, cancel);
            this.messageQueue.Enqueue(record);
            StartDisplay();
            return record.CompletionSource.Task;
        }

        /// <inheritdoc/>
        public Task DisplayAlert(string title, string message, string accept)
        {
            MessageRecord record = new MessageRecord(title, message, accept, null);
            this.messageQueue.Enqueue(record);
            StartDisplay();
            return record.CompletionSource.Task;
        }

        /// <inheritdoc/>
        public Task DisplayAlert(string title, string message)
        {
            MessageRecord record = new MessageRecord(title, message, null, null);
            this.messageQueue.Enqueue(record);
            StartDisplay();
            return record.CompletionSource.Task;
        }

        /// <inheritdoc/>
        public async Task DisplayAlert(string title, string message, Exception exception)
        {
            exception = Log.UnnestException(exception);

            StringBuilder sb = new StringBuilder();
            if (!(exception is null))
            {
                if (exception is AggregateException aggregate && !(aggregate.InnerException is null))
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

        /// <inheritdoc/>
        public Task DisplayAlert(string title, Exception exception)
        {
            return this.DisplayAlert(title, null, exception);
        }

        /// <inheritdoc/>
        public Task DisplayAlert(Exception exception)
        {
            return this.DisplayAlert(AppResources.ErrorTitle, null, exception);
        }

        private async Task DisplayAllMessages()
        {
            try
            {
                do
                {
                    if (this.messageQueue.TryDequeue(out MessageRecord record))
                    {
                        if (string.IsNullOrWhiteSpace(record.Accept) && !string.IsNullOrWhiteSpace(record.Cancel))
                        {
                            await Application.Current.MainPage.DisplayAlert(record.Title, record.Message, record.Cancel);
                            record.CompletionSource.TrySetResult(true);
                        }
                        else if (!string.IsNullOrWhiteSpace(record.Accept) && string.IsNullOrWhiteSpace(record.Cancel))
                        {
                            await Application.Current.MainPage.DisplayAlert(record.Title, record.Message, record.Accept);
                            record.CompletionSource.TrySetResult(true);
                        }
                        else if (string.IsNullOrWhiteSpace(record.Accept) && string.IsNullOrWhiteSpace(record.Cancel))
                        {
                            await Application.Current.MainPage.DisplayAlert(record.Title, record.Message, AppResources.Ok);
                            record.CompletionSource.TrySetResult(true);
                        }
                        else
                        {
                            bool result = await Application.Current.MainPage.DisplayAlert(record.Title, record.Message, record.Accept, record.Cancel);
                            record.CompletionSource.TrySetResult(result);
                        }
                    }
                }
                while (this.messageQueue.Count > 0);
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
                CompletionSource = new TaskCompletionSource<bool>();
            }

            public string Title { get; }
            public string Message { get; }
            public string Accept { get; }
            public string Cancel { get; }

            public TaskCompletionSource<bool> CompletionSource { get; }
        }
    }
}