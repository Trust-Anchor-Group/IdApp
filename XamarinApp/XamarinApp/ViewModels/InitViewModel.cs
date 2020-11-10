using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels
{
    public class InitViewModel : BindableObject
    {
        private readonly ITagService tagService;

        public InitViewModel()
        {
            this.tagService = DependencyService.Resolve<ITagService>();
        }

        public Task Init()
        {
            return this.tagService.Init();
        }
    }
}