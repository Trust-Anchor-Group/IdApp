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

        public async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToClosePage);
            }
        }

        public Task GoToAsync(string route)
        {
            return GoToAsync(route, (NavigationArgs)null);
        }

        public async Task GoToAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs
        {
            this.PushArgs(args);
            try
            {
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToPage, route));
            }
        }

        public Task ReplaceAsync(string route)
        {
            return ReplaceAsync(route, (NavigationArgs)null);
        }

        public async Task ReplaceAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs
        {
            route = $"///{route}";
            this.PushArgs(args);
            try
            {
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToPage, route));
            }
        }
    }
}