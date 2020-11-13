using System;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationStepViewModel : BaseViewModel
    {
        public event EventHandler StepCompleted;

        public RegistrationStepViewModel(int step, ITagService tagService)
        {
            Step = step;
            TagService = tagService;
        }

        public int Step { get; }

        protected ITagService TagService { get; }

        protected virtual void OnStepCompleted(EventArgs e)
        {
            StepCompleted?.Invoke(this, e);
        }
    }
}