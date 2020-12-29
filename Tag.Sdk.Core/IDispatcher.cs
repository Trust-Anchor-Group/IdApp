using System;
using System.Threading.Tasks;

namespace Tag.Sdk.Core
{
    public interface IDispatcher
    {
        void BeginInvokeOnMainThread(Action action);
        Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
        Task DisplayAlert(string title, string message, string accept);
        Task DisplayAlert(string title, string message);
        Task DisplayAlert(string title, string message, Exception exception);
        Task DisplayAlert(string title, Exception exception);
        Task DisplayAlert(Exception exception);
    }
}