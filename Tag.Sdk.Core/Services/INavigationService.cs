using System.Threading.Tasks;

namespace Tag.Sdk.Core.Services
{
    /// <summary>
    /// Handles all page navigation in the app.
    /// </summary>
    public interface INavigationService
    {
        Task ReplaceAsync(string route);
        Task ReplaceAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs;
        Task GoToAsync(string route);
        Task GoToAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs;
        Task GoBackAsync();
        void PushArgs<TArgs>(TArgs args) where TArgs : NavigationArgs;
        bool TryPopArgs<TArgs>(out TArgs args) where TArgs : NavigationArgs;
    }
}