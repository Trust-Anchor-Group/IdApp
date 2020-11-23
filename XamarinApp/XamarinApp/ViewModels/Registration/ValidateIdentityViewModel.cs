using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class ValidateIdentityViewModel : RegistrationStepViewModel
    {
        public ValidateIdentityViewModel(RegistrationStep step, TagProfile tagProfile, INeuronService neuronService,
            IMessageService messageService)
            : base(step, tagProfile, neuronService, messageService)
        {

        }
    }
}