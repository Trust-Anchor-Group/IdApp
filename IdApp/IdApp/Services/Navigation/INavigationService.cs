using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.Services.Navigation
{
    /// <summary>
    /// The navigation service is a wafer-thin wrapper around the <see cref="Shell"/>'s <c>GoToAsync()</c> methods.
    /// It also provides a uniform way of passing arguments to pages.
    /// Allows for easy mocking when unit testing.
    /// </summary>
    [DefaultImplementation(typeof(NavigationService))]
    public interface INavigationService : ILoadableService
    {
        /// <summary>
        /// Navigates the AppShell to the specified route.
        /// </summary>
        /// <param name="route">The route whose matching page to navigate to.</param>
        Task GoToAsync(string route);

        /// <summary>
        /// Navigates the AppShell to the specified route, with page arguments to match.
        /// </summary>
        /// <param name="route">The route whose matching page to navigate to.</param>
        /// <param name="args">The specific args to pass to the page.</param>
        Task GoToAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs, new();

        /// <summary>
        /// Returns to the previous page/route.
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// Returns to the previous page/route.
        /// </summary>
        /// <param name="Animate">If animation should be used.</param>
        Task GoBackAsync(bool Animate);

		/// <summary>
		/// Tries to pop/read page arguments from the (one-level) deep navigation stack.
		/// </summary>
		/// <typeparam name="TArgs">The type of arguments to pass. Must be a subclass of <see cref="NavigationArgs"/>.</typeparam>
		/// <param name="args">The actual args.</param>
		/// <param name="UniqueId">Views unique ID.</param>
		bool TryPopArgs<TArgs>(out TArgs args, string UniqueId = null) where TArgs : NavigationArgs;

		/// <summary>
		/// Returns the pages arguments from the (one-level) deep navigation stack.
		/// </summary>
		/// <param name="UniqueId">Views unique ID.</param>
		TArgs GetPopArgs<TArgs>(string UniqueId = null) where TArgs : NavigationArgs;

        /// <summary>
        /// Current page
        /// </summary>
        Page CurrentPage { get; }
    }
}
