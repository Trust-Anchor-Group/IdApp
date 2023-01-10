using System.Windows.Input;
using Xamarin.Forms;

namespace IdApp.Pages.Registration
{
    /// <summary>
    /// A base class view for all registration steps views. Inherit from this to get access to the <see cref="StepCompletedCommand"/>.
    /// </summary>
    public abstract class RegistrationStepView : ContentBaseView
    {
        /// <summary>
        /// See <see cref="StepCompletedCommand"/>
        /// </summary>
        public static readonly BindableProperty StepCompletedCommandProperty =
            BindableProperty.Create(nameof(StepCompletedCommand), typeof(ICommand), typeof(RegistrationStepView), default(ICommand), BindingMode.OneWayToSource);

        /// <summary>
        /// The command to bind to for signaling to listeners that this step has completed.
        /// </summary>
        public ICommand StepCompletedCommand
        {
            get => (ICommand)this.GetValue(StepCompletedCommandProperty);
            set => this.SetValue(StepCompletedCommandProperty, value);
        }
    }
}