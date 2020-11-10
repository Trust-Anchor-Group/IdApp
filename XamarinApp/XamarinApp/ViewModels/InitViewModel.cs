using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels
{
    public class InitViewModel : BindableObject
    {
        private readonly ITagService _tagService;

        public InitViewModel()
        {
            _tagService = App.TagService;
        }

        public Task Init()
        {
            return _tagService.Init();
        }
    }
}