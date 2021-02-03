using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// The navigation service is a wafer-thin wrapper around the <see cref="Shell"/>'s <c>GoToAsync()</c> methods.
    /// It also provides a uniform way of passing arguments to pages, see the <see cref="PushArgs{TArgs}"/> and <see cref="TryPopArgs{TArgs}"/> methods.
    /// Allows for easy mocking when unit testing.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates the AppShell to the specified route.
        /// </summary>
        /// <param name="route">The route whose matching page to navigate to.</param>
        /// <returns></returns>
        Task GoToAsync(string route);
        /// <summary>
        /// Navigates the AppShell to the specified route, with page arguments to match.
        /// </summary>
        /// <param name="route">The route whose matching page to navigate to.</param>
        /// <param name="args">The specific args to pass to the page.</param>
        /// <returns></returns>
        Task GoToAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs;
        /// <summary>
        /// Returns to the previous page/route.
        /// </summary>
        /// <returns></returns>
        Task GoBackAsync();
        /// <summary>
        /// Pushes page arguments onto the (one-level) deep navigation stack.
        /// </summary>
        /// <typeparam name="TArgs">The type of arguments to pass. Must be a subclass of <see cref="NavigationArgs"/>.</typeparam>
        /// <param name="args">The actual args.</param>
        void PushArgs<TArgs>(TArgs args) where TArgs : NavigationArgs;
        /// <summary>
        /// Tries to pop/read page arguments from the (one-level) deep navigation stack.
        /// </summary>
        /// <typeparam name="TArgs">The type of arguments to pass. Must be a subclass of <see cref="NavigationArgs"/>.</typeparam>
        /// <param name="args">The actual args.</param>
        bool TryPopArgs<TArgs>(out TArgs args) where TArgs : NavigationArgs;
    }
}