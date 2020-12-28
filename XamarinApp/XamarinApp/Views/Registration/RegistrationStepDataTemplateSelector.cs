﻿using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using XamarinApp.Services;
using XamarinApp.ViewModels.Registration;

namespace XamarinApp.Views.Registration
{
    public class RegistrationStepDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ChooseOperator { get; set; }
        public DataTemplate ChooseAccount { get; set; }
        public DataTemplate RegisterIdentity { get; set; }
        public DataTemplate ValidateIdentity { get; set; }
        public DataTemplate DefinePin { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            RegistrationStepViewModel viewModel = (RegistrationStepViewModel)item;

            switch (viewModel.Step)
            {
                case RegistrationStep.Account:
                    return ChooseAccount;
                case RegistrationStep.RegisterIdentity:
                    return RegisterIdentity;
                case RegistrationStep.ValidateIdentity:
                    return ValidateIdentity;
                case RegistrationStep.Pin:
                    return DefinePin;
                default:
                    return ChooseOperator;
            }
        }
    }
}