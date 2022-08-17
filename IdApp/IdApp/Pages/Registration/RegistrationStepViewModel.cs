using System;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.Tag;
using Xamarin.Forms;

namespace IdApp.Pages.Registration
{
    /// <summary>
    /// A base class for all view models that handle any part of the registration flow.
    /// </summary>
    public abstract class RegistrationStepViewModel : BaseViewModel
    {
        /// <summary>
        /// An event that fires whenever the <see cref="Step"/> property changes.
        /// </summary>
        public event EventHandler StepCompleted;

        /// <summary>
        /// Creates a new instance of the <see cref="IdApp.Pages.Registration.RegisterIdentity.RegisterIdentityViewModel"/> class.
        /// </summary>
        /// <param name="step">The current step for this instance.</param>
        public RegistrationStepViewModel(RegistrationStep step)
        {
            this.Step = step;
        }

        #region Properties

        /// <summary>
        /// See <see cref="Title"/>
        /// </summary>
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(RegistrationStepViewModel), default(string));

        /// <summary>
        /// The title to display on the page or view rendering this view model.
        /// </summary>
        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        /// <summary>
        /// The current step for this view model instance.
        /// </summary>
        public RegistrationStep Step { get; }

        #endregion

        /// <summary>
        /// Call this method to fire the <see cref="StepCompleted"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnStepCompleted(EventArgs e)
        {
            this.StepCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// Invoked whenever a registration step needs to be reverted, therefore wiping all persisted UI state.
        /// Override this to clear any settings for the specific step view model.
        /// </summary>
        public virtual void ClearStepState()
        {
        }

		/// <summary>
		/// Override this method to do view model specific of setting the default properties values.
		/// </summary>
		public virtual Task DoAssignProperties()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// A helper method for asynchronously setting this registration step to Done, and also calling
		/// <see cref="Command.ChangeCanExecute"/> on the list of commands passed in.
		/// </summary>
		/// <param name="commands">The commands to re-evaluate.</param>
		protected void BeginInvokeSetIsDone(params ICommand[] commands)
        {
            this.UiSerializer.BeginInvokeOnMainThread(() => this.SetIsDone(commands));
        }
    }
}
