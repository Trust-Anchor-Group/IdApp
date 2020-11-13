using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinApp.Services
{
    public interface INavigationService
    {
        Task<Page> GoBack();
        Task<Page> GoBackFromModal();
        Task GoTo(Page page);
        Task Set(Page page);
    }
}