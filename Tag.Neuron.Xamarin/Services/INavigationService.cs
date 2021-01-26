using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// Handles all page navigation in the app.
    /// </summary>
    public interface INavigationService
    {
        Task GoToAsync(string route);
        Task GoToAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs;
        Task GoBackAsync();
        void PushArgs<TArgs>(TArgs args) where TArgs : NavigationArgs;
        bool TryPopArgs<TArgs>(out TArgs args) where TArgs : NavigationArgs;
    }
}