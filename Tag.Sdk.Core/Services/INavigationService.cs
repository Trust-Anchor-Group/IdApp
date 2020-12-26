using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Sdk.Core.Services
{
    /// <summary>
    /// Handles all page navigation in the app, as well as display alerts.
    /// </summary>
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
        Task<bool> DisplayPrompt(string title, string message, string accept, string cancel);
    }
}