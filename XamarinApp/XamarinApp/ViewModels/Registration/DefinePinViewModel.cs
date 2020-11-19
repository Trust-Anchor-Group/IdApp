using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class DefinePinViewModel : RegistrationStepViewModel
    {
        public DefinePinViewModel(RegistrationStep step, TagProfile tagProfile, ITagService tagService, IMessageService messageService)
            : base(step, tagProfile, tagService, messageService)
        {
        }
    }
}