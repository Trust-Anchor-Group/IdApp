using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinApp.Services
{
    internal sealed class NavigationService : INavigationService
    {
        public Task<Page> GoBack()
        {
            return App.Instance.MainPage.Navigation.PopAsync(true);
        }

        public Task<Page> GoBackFromModal()
        {
            return App.Instance.MainPage.Navigation.PopModalAsync(true);
        }

        public Task GoTo(Page page)
        {
            return App.Instance.MainPage.Navigation.PushAsync(page);
        }

        public Task ShowModal(Page page)
        {
            return App.Instance.MainPage.Navigation.PushModalAsync(page);
        }

        public Task HideModal()
        {
            return App.Instance.MainPage.Navigation.PopModalAsync();
        }

        public async Task Set(Page page)
        {
            // Neat trick to replace current page but still get a page animation.
            Page currPage = App.Instance.MainPage;
            if (currPage is NavigationPage navPage)
            {
                currPage = navPage.CurrentPage;
            }
            await App.Instance.MainPage.Navigation.PushAsync(page);
            currPage.Navigation.RemovePage(currPage);
        }
    }
}