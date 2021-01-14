using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Tag.Sdk.Core.Services
{
    internal sealed class NavigationService : INavigationService
    {
        private readonly List<object> navigationArgs;

        public NavigationService()
        {
            this.navigationArgs = new List<object>();   
        }

        public void PushArgs(params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                this.navigationArgs.AddRange(args);
            }
        }

        public void PopArgs<T1>(out T1 t1) where T1 : class
        {
            t1 = default;

            if (this.navigationArgs.Count > 0)
            {
                t1 = this.navigationArgs[0] as T1;
            }

            this.navigationArgs.Clear();
        }

        public void PopArgs<T1, T2>(out T1 t1, out T2 t2) 
            where T1 : class
            where T2 : class
        {
            t1 = default;
            t2 = default;

            if (this.navigationArgs.Count > 0)
            {
                t1 = this.navigationArgs[0] as T1;
            }
            if (this.navigationArgs.Count > 1)
            {
                t2 = this.navigationArgs[1] as T2;
            }

            this.navigationArgs.Clear();
        }

        public void PopArgs<T1, T2, T3>(out T1 t1, out T2 t2, out T3 t3) 
            where T1 : class
            where T2 : class
            where T3 : class
        {
            t1 = default;
            t2 = default;
            t3 = default;

            if (this.navigationArgs.Count > 0)
            {
                t1 = this.navigationArgs[0] as T1;
            }
            if (this.navigationArgs.Count > 1)
            {
                t2 = this.navigationArgs[1] as T2;
            }
            if (this.navigationArgs.Count > 2)
            {
                t3 = this.navigationArgs[2] as T3;
            }

            this.navigationArgs.Clear();
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

        public async Task ReplaceAsync(string route)
        {
            string uri = $"//{route}";
            await Shell.Current.GoToAsync(uri);
        }

        public async Task ReplaceAsync(Page page)
        {
            //// Neat trick to replace current page but still get a page animation.
            //Page currPage = Application.Current.MainPage;
            //if (currPage is NavigationPage navPage)
            //{
            //    currPage = navPage.CurrentPage;
            //}
            //await Application.Current.MainPage.Navigation.PushAsync(page);
            //currPage.Navigation.RemovePage(currPage);
        }
    }
}