using System.Threading.Tasks;

namespace XamarinApp.Services
{
    public interface IMessageService
    {
        Task DisplayAlert(string title, string message, string accept, string cancel);
        Task DisplayAlert(string title, string message, string cancel);
        Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
    }
}