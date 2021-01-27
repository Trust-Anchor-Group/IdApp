using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Helper method to wait for a task to complete, but with a given time limit.
        /// </summary>
        /// <typeparam name="TResult">The task's result.</typeparam>
        /// <param name="task">The task to await.</param>
        /// <param name="timeout">The maximum time to wait for the task.</param>
        /// <returns></returns>
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using (CancellationTokenSource tcs = new CancellationTokenSource())
            {
                Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, tcs.Token));
                if (completedTask == task)
                {
                    tcs.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                }
                throw new TimeoutException();
            }
        }
    }
}