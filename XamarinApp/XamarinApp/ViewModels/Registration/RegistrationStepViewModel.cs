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

        public RegistrationStepViewModel(
            RegistrationStep step, 
            TagProfile tagProfile, 
            INeuronService neuronService, 
            INavigationService navigationService,
            ISettingsService settingsService)
        {
            this.Step = step;
            this.TagProfile = tagProfile;
            this.NeuronService = neuronService;
            this.NavigationService = navigationService;
            this.SettingsService = settingsService;
        }

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create("Title", typeof(string), typeof(RegistrationStepViewModel), default(string));

        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public RegistrationStep Step { get; }

        protected TagProfile TagProfile { get; }
        protected INeuronService NeuronService { get; }
        protected INavigationService NavigationService { get; }
        protected ISettingsService SettingsService { get; }

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

        protected void SetIsBusy(params ICommand[] commands)
        {
            IsBusy = true;
            foreach (ICommand command in commands)
            {
                command.ChangeCanExecute();
            }
        }
    }
}