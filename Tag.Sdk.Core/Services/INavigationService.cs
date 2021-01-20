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
        Task PushAsync<TArgs>(Page page, TArgs args) where TArgs : NavigationArgs;
        Task PushModalAsync(Page page);
        Task PushModalAsync<TArgs>(Page page, TArgs args) where TArgs : NavigationArgs;

        Task PopAsync();
        Task PopModalAsync();

        Task ReplaceAsync(Page page);
        Task ReplaceAsync<TArgs>(Page page, TArgs args) where TArgs : NavigationArgs;

        Task GoToAsync(string route);
        Task GoToAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs;

        void PushArgs<TArgs>(TArgs args) where TArgs : NavigationArgs;
        bool TryPopArgs<TArgs>(out TArgs args) where TArgs : NavigationArgs;
    }
}