using System;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationStepViewModel : BaseViewModel
    {
        public event EventHandler StepCompleted;

        public RegistrationStepViewModel(RegistrationStep step, TagProfile tagProfile, ITagService tagService)
        {
            Step = step;
            TagProfile = tagProfile;
            TagService = tagService;
        }

        public RegistrationStep Step { get; }

        protected TagProfile TagProfile { get; }
        protected ITagService TagService { get; }

        protected virtual void OnStepCompleted(EventArgs e)
        {
            StepCompleted?.Invoke(this, e);
        }
    }
}