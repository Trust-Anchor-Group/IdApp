using System;
using System.Collections.Generic;
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
        private NavigationArgs currentNavigationArgs;
        private readonly Dictionary<string, NavigationArgs> navigationArgsMap;

        public NavigationService(ILogService logService, IUiDispatcher uiDispatcher)
        {
            this.logService = logService;
            this.uiDispatcher = uiDispatcher;
            this.navigationArgsMap = new Dictionary<string, NavigationArgs>();
        }

        private bool TryGetPageName(string route, out string pageName)
        {
            pageName = null;
            if (!string.IsNullOrWhiteSpace(route))
            {
                pageName = route.TrimStart('.', '/');
                return !string.IsNullOrWhiteSpace(pageName);
            }

            return false;
        }

        internal void PushArgs<TArgs>(string route, TArgs args) where TArgs : NavigationArgs
        {
            this.currentNavigationArgs = args;

            if (this.TryGetPageName(route, out string pageName))
            {
                if (args != null)
                {
                    this.navigationArgsMap[pageName] = args;
                }
                else
                {
                    this.navigationArgsMap.Remove(pageName);
                }
            }
        }

        public bool TryPopArgs<TArgs>(out TArgs args) where TArgs : NavigationArgs
        {
            string route = Shell.Current.CurrentPage?.GetType().Name;
            return this.TryPopArgs(route, out args);
        }

        internal bool TryPopArgs<TArgs>(string route, out TArgs args) where TArgs : NavigationArgs
        {
            args = default;
            if (this.TryGetPageName(route, out string pageName) && 
                this.navigationArgsMap.TryGetValue(pageName, out NavigationArgs navArgs) &&
                !(navArgs is null))
            {
                args = navArgs as TArgs;
            }

            return !(args is null);
        }

        public async Task GoBackAsync()
        {
            try
            {
                string route = (this.currentNavigationArgs != null && !string.IsNullOrWhiteSpace(this.currentNavigationArgs.ReturnRoute)) ? this.currentNavigationArgs.ReturnRoute : "..";
                await Shell.Current.GoToAsync(route);
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
            this.PushArgs(route, args);
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