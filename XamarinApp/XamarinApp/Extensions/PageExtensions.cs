using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinApp.ViewModels;

namespace XamarinApp.Extensions
{
    public static class PageExtensions
    {
        public static async Task BindViewModel(this Element element, BaseViewModel vm)
        {
            if (vm != null)
            {
                await vm.Bind();
            }
        }

        public static async Task UnbindViewModel(this Element element, BaseViewModel vm)
        {
            if (vm != null)
            {
                await vm.Unbind();
            }
        }
    }
}