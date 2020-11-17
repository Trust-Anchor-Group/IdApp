using System;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationStepViewModel : BaseViewModel
    {
        public event EventHandler StepCompleted;

        public RegistrationStepViewModel(int step, TagServiceSettings tagSettings, ITagService tagService)
        {
            Step = step;
            TagSettings = tagSettings;
            TagService = tagService;
        }

        public int Step { get; }

        protected TagServiceSettings TagSettings { get; }
        protected ITagService TagService { get; }

        protected virtual void OnStepCompleted(EventArgs e)
        {
            StepCompleted?.Invoke(this, e);
        }
    }
}