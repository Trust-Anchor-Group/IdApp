using System;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationStepViewModel : BaseViewModel
    {
        public event EventHandler StepCompleted;

        public RegistrationStepViewModel(RegistrationStep step, TagProfile tagProfile, ITagService tagService, IMessageService messageService)
        {
            this.Step = step;
            this.TagProfile = tagProfile;
            this.TagService = tagService;
            this.MessageService = messageService;
        }

        public RegistrationStep Step { get; }

        protected TagProfile TagProfile { get; }
        protected ITagService TagService { get; }
        protected IMessageService MessageService { get; }

        protected virtual void OnStepCompleted(EventArgs e)
        {
            this.StepCompleted?.Invoke(this, e);
        }


        protected void BeginInvokeSetIsDone(params ICommand[] commands)
        {
            Device.BeginInvokeOnMainThread(() => SetIsDone(commands));
        }

        protected void SetIsDone(params ICommand[] commands)
        {
            IsBusy = false;
            foreach (ICommand command in commands)
            {
                command.ChangeCanExecute();
            }
        }
    }
}