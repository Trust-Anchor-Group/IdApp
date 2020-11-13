using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class ChooseAccountViewModel : RegistrationStepViewModel
    {
        public ChooseAccountViewModel(int step, ITagService tagService)
            : base(step, tagService)
        {
        }
    }
}