using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Sdk.Core.Services
{
    internal sealed class NavigationService : INavigationService
    {
        private readonly ILogService logService;
        private readonly IUiDispatcher uiDispatcher;
        private NavigationArgs navigationArgs;

        public NavigationService(ILogService logService, IUiDispatcher uiDispatcher)
        {
            this.logService = logService;
            this.uiDispatcher = uiDispatcher;
        }

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

            // Reset to null
            this.navigationArgs = null;

            return args != null;
        }

        public Task PushAsync(Page page)
        {
            return this.PushAsync(page, (NavigationArgs)null);
        }

        public async Task PushAsync<TArgs>(Page page, TArgs args) where TArgs : NavigationArgs
        {
            this.PushArgs(args);
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(page, true);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToPage, page.GetType().Name));
            }
        }

        public async Task PopAsync()
        {
            try
            {
                await Application.Current.MainPage.Navigation.PopAsync(true);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToClosePage);
            }
        }

        public Task PushModalAsync(Page page)
        {
            return PushModalAsync(page, (NavigationArgs)null);
        }

        public async Task PushModalAsync<TArgs>(Page page, TArgs args) where TArgs : NavigationArgs
        {
            this.PushArgs(args);
            try
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(page, true);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToModalPage, page.GetType().Name));
            }
        }

        public async Task PopModalAsync()
        {
            try
            {
                await Application.Current.MainPage.Navigation.PopModalAsync(true);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToCloseModalPage);
            }
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
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(page);
                currPage.Navigation.RemovePage(currPage);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToPage, page.GetType().Name));
            }
        }
    }
}