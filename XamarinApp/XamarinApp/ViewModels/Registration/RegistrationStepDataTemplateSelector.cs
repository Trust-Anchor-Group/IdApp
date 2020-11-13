using Xamarin.Forms;

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
                case 1:
                    return Step2Template;
                case 2:
                    return Step3Template;
                case 3:
                    return Step4Template;
                case 4:
                    return Step5Template;
                default:
                    return Step1Template;
            }
        }
    }
}