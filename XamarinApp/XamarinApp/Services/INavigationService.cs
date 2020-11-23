using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinApp.Views;

namespace XamarinApp.Services
{
    public interface INavigationService
    {
        Task<Page> GoBack();
        Task<Page> GoBackFromModal();
        Task GoTo(Page page);
        Task Set(Page page);
        Task ShowModal(Page page);
        Task HideModal();
    }
}