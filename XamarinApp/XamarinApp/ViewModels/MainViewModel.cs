using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly TagProfile tagProfile;
        private readonly ITagService tagService;

        public MainViewModel()
        {
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.tagService = DependencyService.Resolve<ITagService>();
        }
    }
}