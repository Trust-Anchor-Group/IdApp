using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly TagProfile tagProfile;
        private readonly INeuronService neuronService;

        public MainViewModel()
        {
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.neuronService = DependencyService.Resolve<INeuronService>();
        }
    }
}