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

        void PushArgs<TArgs>(TArgs args) where TArgs : NavigationArgs;
        bool TryPopArgs<TArgs>(out TArgs args) where TArgs : NavigationArgs;
    }
}