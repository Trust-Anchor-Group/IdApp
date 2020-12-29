using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Sdk.Core.Services
{
    internal sealed class NavigationService : INavigationService
    {
        private readonly IDispatcher dispatcher;

        public NavigationService(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public Task PushAsync(Page page)
        {
            return Application.Current.MainPage.Navigation.PushAsync(page, true);
        }

        public Task<Page> PopAsync()
        {
            return Application.Current.MainPage.Navigation.PopAsync(true);
        }

        public Task PushModalAsync(Page page)
        {
            return Application.Current.MainPage.Navigation.PushModalAsync(page, true);
        }

        public Task PopModalAsync()
        {
            return Application.Current.MainPage.Navigation.PopModalAsync(true);
        }

        public async Task ReplaceAsync(Page page)
        {
            // Neat trick to replace current page but still get a page animation.
            Page currPage = Application.Current.MainPage;
            if (currPage is NavigationPage navPage)
            {
                currPage = navPage.CurrentPage;
            }
            await Application.Current.MainPage.Navigation.PushAsync(page);
            currPage.Navigation.RemovePage(currPage);
        }
    }
}