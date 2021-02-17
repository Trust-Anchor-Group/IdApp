using IdApp.ViewModels.Registration;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;

namespace IdApp.Views.Registration
{
    /// <summary>
    /// A data template selector for displaying various types of registration steps.
    /// </summary>
    public class RegistrationStepDataTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// The choose operator template.
        /// </summary>
        public DataTemplate ChooseOperator { get; set; }
        /// <summary>
        /// The choose account template.
        /// </summary>
        public DataTemplate ChooseAccount { get; set; }
        /// <summary>
        /// The register identity template.
        /// </summary>
        public DataTemplate RegisterIdentity { get; set; }
        /// <summary>
        /// The validate identity template.
        /// </summary>
        public DataTemplate ValidateIdentity { get; set; }
        /// <summary>
        /// The define pin template.
        /// </summary>
        public DataTemplate DefinePin { get; set; }

        /// <summary>
        /// Chooses the best matching data template based on the type of registration step.
        /// </summary>
        /// <param name="item">The step to display.</param>
        /// <param name="container"></param>
        /// <returns></returns>
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