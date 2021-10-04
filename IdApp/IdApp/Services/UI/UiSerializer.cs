using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using IdApp.Services.UI.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.Services.UI
{
    /// <inheritdoc/>
    [Singleton]
    public class UiSerializer : IUiSerializer
    {
        private readonly ConcurrentQueue<UiTask> taskQueue;
        private bool isExecutingUiTasks;

        /// <summary>
        /// Creates a new instance of the <see cref="UiSerializer"/> class.
        /// </summary>
        public UiSerializer()
        {
            this.taskQueue = new ConcurrentQueue<UiTask>();

            this.isExecutingUiTasks = false;
            this.IsRunningInTheBackground = false;
        }

        /// <inheritdoc/>
        public void BeginInvokeOnMainThread(Action action)
        {
            Device.BeginInvokeOnMainThread(action);
        }

        /// <inheritdoc/>
        public bool IsRunningInTheBackground { get; set; }

        private void AddTask(UiTask Task)
		{
            this.taskQueue.Enqueue(Task);

            if (!this.isExecutingUiTasks)
            {
                this.isExecutingUiTasks = true;

                this.BeginInvokeOnMainThread(async () =>
                {
                    await ProcessAllTasks();
                });
            }
        }

        private async Task ProcessAllTasks()
        {
            try
            {
                do
                {
                    if (this.taskQueue.TryDequeue(out UiTask Task))
                        await Task.Execute();
                }
                while (this.taskQueue.Count > 0);
            }
            finally
            {
                this.isExecutingUiTasks = false;
            }
        }

		#region DisplayAlert

		/// <inheritdoc/>
		public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            DisplayAlert Task = new DisplayAlert(title, message, accept, cancel);
            this.AddTask(Task);
            return Task.CompletionSource.Task;
        }

        /// <inheritdoc/>
        public Task DisplayAlert(string title, string message, string accept)
        {
            DisplayAlert Task = new DisplayAlert(title, message, accept, null);
            this.AddTask(Task);
            return Task.CompletionSource.Task;
        }

        /// <inheritdoc/>
        public Task DisplayAlert(string title, string message)
        {
            DisplayAlert Task = new DisplayAlert(title, message, null, null);
            this.AddTask(Task);
            return Task.CompletionSource.Task;
        }

        /// <inheritdoc/>
        public Task DisplayAlert(string title, string message, Exception exception)
        {
            exception = Log.UnnestException(exception);

            StringBuilder sb = new StringBuilder();

            if (!(exception is null))
            {
                sb.AppendLine(exception.Message);

                while ((exception = exception.InnerException) != null)
                    sb.AppendLine(exception.Message);
            }
            else
                sb.AppendLine(AppResources.AnErrorHasOccurred);

            return this.DisplayAlert(title ?? AppResources.ErrorTitle, sb.ToString(), AppResources.Ok);
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

        #endregion

        #region DisplayPrompt

        /// <inheritdoc/>
        public Task<string> DisplayPrompt(string title, string message, string accept, string cancel)
        {
            DisplayPrompt Task = new DisplayPrompt(title, message, accept, cancel);
            this.AddTask(Task);
            return Task.CompletionSource.Task;
        }

        /// <inheritdoc/>
        public Task DisplayPrompt(string title, string message, string accept)
        {
            DisplayPrompt Task = new DisplayPrompt(title, message, accept, null);
            this.AddTask(Task);
            return Task.CompletionSource.Task;
        }

        /// <inheritdoc/>
        public Task DisplayPrompt(string title, string message)
        {
            DisplayPrompt Task = new DisplayPrompt(title, message, null, null);
            this.AddTask(Task);
            return Task.CompletionSource.Task;
        }

        #endregion

    }
}