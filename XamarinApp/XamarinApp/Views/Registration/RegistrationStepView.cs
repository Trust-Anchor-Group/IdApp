using System.Windows.Input;
using Tag.Neuron.Xamarin.UI.Views;
using Xamarin.Forms;

namespace XamarinApp.Views.Registration
{
    public class RegistrationStepView : ContentBaseView
    {
        public static readonly BindableProperty StepCompletedCommandProperty =
            BindableProperty.Create("StepCompletedCommand", typeof(ICommand), typeof(RegistrationStepView), default(ICommand), BindingMode.OneWayToSource);

        public ICommand StepCompletedCommand
        {
            get { return (ICommand)GetValue(StepCompletedCommandProperty); }
            set { SetValue(StepCompletedCommandProperty, value); }
        }
    }
}