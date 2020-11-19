using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class ValidateIdentityViewModel : RegistrationStepViewModel
    {
        public ValidateIdentityViewModel(RegistrationStep step, TagProfile tagProfile, ITagService tagService,
            IMessageService messageService)
            : base(step, tagProfile, tagService, messageService)
        {

        }
    }
}