using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels.Registration
{
    public class RegistrationStepDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Step1Template { get; set; }
        public DataTemplate Step2Template { get; set; }
        public DataTemplate Step3Template { get; set; }
        public DataTemplate Step4Template { get; set; }
        public DataTemplate Step5Template { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            RegistrationStepViewModel viewModel = (RegistrationStepViewModel)item;

            switch (viewModel.Step)
            {
                case RegistrationStep.Account:
                    return Step2Template;
                case RegistrationStep.RegisterIdentity:
                    return Step3Template;
                case RegistrationStep.ValidateIdentity:
                    return Step4Template;
                case RegistrationStep.Pin:
                    return Step5Template;
                default:
                    return Step1Template;
            }
        }
    }
}