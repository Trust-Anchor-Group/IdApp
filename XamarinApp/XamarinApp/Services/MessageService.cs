using System.Threading.Tasks;

namespace XamarinApp.Services
{
    internal sealed class MessageService : IMessageService
    {
        public Task DisplayAlert(string title, string message, string accept, string cancel)
        {
            return App.Instance.MainPage.DisplayAlert(title, message, accept, cancel);
        }

        public Task DisplayAlert(string title, string message, string cancel)
        {
            return App.Instance.MainPage.DisplayAlert(title, message, cancel);
        }
    }
}