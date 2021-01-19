using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Sdk.Core.Services
{
    internal sealed class NavigationService : INavigationService
    {
        private NavigationArgs navigationArgs;

        public void PushArgs<TArgs>(TArgs args) where TArgs : NavigationArgs
        {
            this.navigationArgs = args;
        }

        public bool TryPopArgs<TArgs>(out TArgs args) where TArgs : NavigationArgs
        {
            args = default;

            if (this.navigationArgs != null)
            {
                args = this.navigationArgs as TArgs;
            }

            this.ClearArgs();
            return args != null;
        }

        private void ClearArgs()
        {
            this.navigationArgs = null;
        }

        public Task PushAsync(Page page)
        {
            this.ClearArgs();
            return Application.Current.MainPage.Navigation.PushAsync(page, true);
        }

        public Task PushAsync<TArgs>(Page page, TArgs args) where TArgs : NavigationArgs
        {
            this.PushArgs(args);
            return Application.Current.MainPage.Navigation.PushAsync(page, true);
        }

        public Task<Page> PopAsync()
        {
            return Application.Current.MainPage.Navigation.PopAsync(true);
        }

        public Task PushModalAsync(Page page)
        {
            this.ClearArgs();
            return Application.Current.MainPage.Navigation.PushModalAsync(page, true);
        }

        public Task PushModalAsync<TArgs>(Page page, TArgs args) where TArgs : NavigationArgs
        {
            this.PushArgs(args);
            return Application.Current.MainPage.Navigation.PushModalAsync(page, true);
        }

        public Task PopModalAsync()
        {
            return Application.Current.MainPage.Navigation.PopModalAsync(true);
        }

        public Task ReplaceAsync(Page page)
        {
            return ReplaceAsync(page, (NavigationArgs)null);
        }

        public async Task ReplaceAsync<TArgs>(Page page, TArgs args) where TArgs : NavigationArgs
        {
            // Neat trick to replace current page but still get a page animation.
            Page currPage = Application.Current.MainPage;
            if (currPage is NavigationPage navPage)
            {
                currPage = navPage.CurrentPage;
            }

            this.PushArgs(args);
            await Application.Current.MainPage.Navigation.PushAsync(page);
            currPage.Navigation.RemovePage(currPage);
        }

        public async Task ReplaceAsync(string route)
        {
            string uri = $"///{route}";
            await Shell.Current.GoToAsync(uri);
        }

        public async Task ReplaceAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs
        {
            this.PushArgs(args);
            string uri = $"///{route}";
            await Shell.Current.GoToAsync(uri);
        }
    }
}