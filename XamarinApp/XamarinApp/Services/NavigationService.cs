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
	}
}