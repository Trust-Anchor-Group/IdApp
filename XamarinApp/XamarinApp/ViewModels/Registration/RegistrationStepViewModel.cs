﻿using System;
using System.Windows.Input;
using Tag.Sdk.Core.Services;
using Tag.Sdk.UI.Extensions;
using Tag.Sdk.UI.ViewModels;
using Xamarin.Forms;
using IDispatcher = Tag.Sdk.Core.IDispatcher;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationStepViewModel : BaseViewModel
    {
        public event EventHandler StepCompleted;

        public RegistrationStepViewModel(
            RegistrationStep step, 
            TagProfile tagProfile,
            IDispatcher dispatcher,
            INeuronService neuronService, 
            INavigationService navigationService,
            ISettingsService settingsService,
            ILogService logService)
        {
            this.Step = step;
            this.Dispatcher = dispatcher;
            this.TagProfile = tagProfile;
            this.NeuronService = neuronService;
            this.NavigationService = navigationService;
            this.SettingsService = settingsService;
            this.LogService = logService;
        }

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create("Title", typeof(string), typeof(RegistrationStepViewModel), default(string));

        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public RegistrationStep Step { get; }

        protected IDispatcher Dispatcher { get; }
        protected TagProfile TagProfile { get; }
        protected INeuronService NeuronService { get; }
        protected INavigationService NavigationService { get; }
        protected ISettingsService SettingsService { get; }
        protected ILogService LogService { get; }

        protected virtual void OnStepCompleted(EventArgs e)
        {
            this.StepCompleted?.Invoke(this, e);
        }


        protected void BeginInvokeSetIsDone(params ICommand[] commands)
        {
            Dispatcher.BeginInvokeOnMainThread(() => SetIsDone(commands));
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