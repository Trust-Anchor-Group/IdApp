using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinApp.Views;

namespace XamarinApp.Services
{
    public interface INavigationService
    {
        Task PushAsync(Page page);
        Task<Page> PopAsync();
        Task PushModalAsync(Page page);
        Task PopModalAsync();
        Task ReplaceAsync(Page page);

        Task DisplayAlert(string title, string message, string accept, string cancel);
        Task DisplayAlert(string title, string message, string accept);
        Task DisplayAlert(string title, string message);
        Task DisplayAlert(string title, string message, Exception exception);
        Task DisplayAlert(string title, Exception exception);
        Task DisplayAlert(Exception exception);
    }
}