using System;
using System.Threading.Tasks;

namespace XamarinApp.Services
{
    public interface IMessageService
    {
        Task DisplayAlert(string title, string message, string accept, string cancel);
        Task DisplayAlert(string title, string message, string accept);
        Task DisplayAlert(string title, string message);
        Task DisplayAlert(Exception exception);
    }
}