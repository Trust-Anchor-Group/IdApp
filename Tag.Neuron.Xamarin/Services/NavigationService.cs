using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.Services
{
    [Singleton]
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

        internal void PushArgs<TArgs>(TArgs args) where TArgs : NavigationArgs
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
                string extraInfo = $"{Environment.NewLine}{e.Message}";
                await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToPage, route, extraInfo));
            }
        }
    }
}