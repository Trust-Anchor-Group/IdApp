using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tag.Sdk.Core.Extensions
{
    public static class TaskExtensions
    {
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