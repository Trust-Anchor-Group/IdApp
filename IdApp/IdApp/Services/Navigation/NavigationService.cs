using IdApp.Services.EventLog;
using IdApp.Services.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.Services.Navigation
{
    [Singleton]
    internal sealed class NavigationService : LoadableService, INavigationService
    {
        private const string DefaultGoBackRoute = "..";
        private readonly ILogService logService;
        private readonly IUiSerializer uiSerializer;
        private NavigationArgs currentNavigationArgs;
        private readonly Dictionary<string, NavigationArgs> navigationArgsMap;
        bool isManuallyNavigatingBack = false;

        public NavigationService(ILogService logService, IUiSerializer uiSerializer)
        {
            this.logService = logService;
            this.uiSerializer = uiSerializer;
            this.navigationArgsMap = new Dictionary<string, NavigationArgs>();
        }

        ///<inheritdoc/>
        public override Task Load(bool isResuming)
        {
            if (this.BeginLoad())
            {
                try
                {
                    Shell.Current.Navigating += Shell_Navigating;

                    this.EndLoad(true);
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);
                    this.EndLoad(false);
                }
            }

            return Task.CompletedTask;
        }

        ///<inheritdoc/>
        public override Task Unload()
        {
            if (this.BeginUnload())
            {
                try
                {
                    Shell.Current.Navigating -= Shell_Navigating;
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);
                }

                this.EndUnload();
            }
        
            return Task.CompletedTask;
        }


        private async void Shell_Navigating(object sender, ShellNavigatingEventArgs e)
        {
            string customGoBackRoute = (this.currentNavigationArgs != null && !string.IsNullOrWhiteSpace(this.currentNavigationArgs.ReturnRoute)) ? this.currentNavigationArgs.ReturnRoute : DefaultGoBackRoute;
            string path = e.Target.Location.ToString();
            if (path == DefaultGoBackRoute && // user wants to go back
                customGoBackRoute != DefaultGoBackRoute && // we have a custom back route to use instead of the default one
                e.CanCancel && // Can we cancel navigation?
                !this.isManuallyNavigatingBack) // Avoid recursion
            {
                this.isManuallyNavigatingBack = true;
                e.Cancel();
                await this.GoBackAsync();
                this.isManuallyNavigatingBack = false;
            }
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

        public Task GoBackAsync()
        {
            return this.GoBackAsync(true);
        }

        public async Task GoBackAsync(bool Animate)
        {
            try
            {
                string route = (this.currentNavigationArgs != null && !string.IsNullOrWhiteSpace(this.currentNavigationArgs.ReturnRoute)) ?
                    this.currentNavigationArgs.ReturnRoute : DefaultGoBackRoute;

                await Shell.Current.GoToAsync(route, Animate);
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
                await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToClosePage);
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
                await Shell.Current.GoToAsync(route, true);
            }
            catch (Exception e)
            {
                e = Log.UnnestException(e);
                this.logService.LogException(e);
                string extraInfo = $"{Environment.NewLine}{e.Message}";
                await this.uiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToPage, route, extraInfo));
            }
        }
    }
}