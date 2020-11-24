﻿using System;
using System.Windows.Input;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class DefinePinViewModel : RegistrationStepViewModel
    {
        public DefinePinViewModel(
            TagProfile tagProfile,
            INeuronService neuronService,
            IMessageService messageService)
            : base(RegistrationStep.Pin, tagProfile, neuronService, messageService)
        {
            PinChangedCommand = new Command<string>(s => Pin = s);
            RetypedPinChangedCommand = new Command<string>(s => RetypedPin = s);
            ContinueCommand = new Command(_ => Continue(), _ => CanContinue());
            SkipCommand = new Command(_ => Skip());
        }

        public ICommand PinChangedCommand { get; }
        public ICommand RetypedPinChangedCommand { get; }
        public ICommand ContinueCommand { get; }
        public ICommand SkipCommand { get; }

        public static readonly BindableProperty PinProperty =
            BindableProperty.Create("Pin", typeof(string), typeof(DefinePinViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
        {
            DefinePinViewModel viewModel = (DefinePinViewModel)b;
            viewModel.ContinueCommand.ChangeCanExecute();
            viewModel.UpdatePinState();
        });

        public string Pin
        {
            get { return (string)GetValue(PinProperty); }
            set { SetValue(PinProperty, value); }
        }

        public static readonly BindableProperty RetypedPinProperty =
            BindableProperty.Create("RetypedPin", typeof(string), typeof(DefinePinViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
            {
                DefinePinViewModel viewModel = (DefinePinViewModel)b;
                viewModel.ContinueCommand.ChangeCanExecute();
                viewModel.UpdatePinState();
            });

        public string RetypedPin
        {
            get { return (string)GetValue(RetypedPinProperty); }
            set { SetValue(RetypedPinProperty, value); }
        }

        private void UpdatePinState()
        {
            PinsDoNotMatch = (Pin != RetypedPin);
        }

        public static readonly BindableProperty PinsDoNotMatchProperty =
            BindableProperty.Create("PinsDoNotMatch", typeof(bool), typeof(DefinePinViewModel), default(bool));

        public bool PinsDoNotMatch
        {
            get { return (bool)GetValue(PinsDoNotMatchProperty); }
            set { SetValue(PinsDoNotMatchProperty, value); }
        }

        public static readonly BindableProperty UsePinProperty =
            BindableProperty.Create("UsePin", typeof(bool), typeof(DefinePinViewModel), default(bool));

        public bool UsePin
        {
            get { return (bool)GetValue(UsePinProperty); }
            set { SetValue(UsePinProperty, value); }
        }

        private void Skip()
        {
            UsePin = false;
            OnStepCompleted(EventArgs.Empty);
        }

        private void Continue()
        {
            UsePin = true;

            // TODO: validation here.

            OnStepCompleted(EventArgs.Empty);
        }

        private bool CanContinue()
        {
            // TODO: validation here
            return false;
        }
    }
}