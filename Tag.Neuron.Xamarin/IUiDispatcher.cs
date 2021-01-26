using System;
using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin
{
    public interface IUiDispatcher
    {
        void BeginInvokeOnMainThread(Action action);
        bool IsRunningInTheBackground { get; }
        Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
        Task DisplayAlert(string title, string message, string accept);
        Task DisplayAlert(string title, string message);
        Task DisplayAlert(string title, string message, Exception exception);
        Task DisplayAlert(string title, Exception exception);
        Task DisplayAlert(Exception exception);
    }
}