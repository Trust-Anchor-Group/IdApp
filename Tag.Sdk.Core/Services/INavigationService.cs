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
        Task ReplaceAsync(string route);

        void PushArgs(params object[] args);
        void PopArgs<T1>(out T1 t1) where T1 : class;
        void PopArgs<T1, T2>(out T1 t1, out T2 t2) where T1 : class where T2 : class;
        void PopArgs<T1, T2, T3>(out T1 t1, out T2 t2, out T3 t3) where T1 : class where T2 : class where T3 : class;
    }
}