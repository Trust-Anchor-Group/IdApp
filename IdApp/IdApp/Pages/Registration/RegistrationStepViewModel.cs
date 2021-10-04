using System;
using System.Windows.Input;
using IdApp;
using IdApp.Services;
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
        /// <param name="tagProfile">The tag profile to work with.</param>
        /// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
        /// <param name="neuronService">The Neuron service for XMPP communication.</param>
        /// <param name="navigationService">The navigation service to use for app navigation</param>
        /// <param name="settingsService">The settings service for persisting UI state.</param>
        /// <param name="logService">The log service.</param>
        public RegistrationStepViewModel(
            RegistrationStep step,
            ITagProfile tagProfile,
            IUiDispatcher uiDispatcher,
            INeuronService neuronService, 
            INavigationService navigationService,
            ISettingsService settingsService,
            ILogService logService)
        {
            this.Step = step;
            this.UiDispatcher = uiDispatcher;
            this.TagProfile = tagProfile;
            this.NeuronService = neuronService;
            this.NavigationService = navigationService;
            this.SettingsService = settingsService;
            this.LogService = logService;
        }

        #region Properties

        /// <summary>
        /// See <see cref="Title"/>
        /// </summary>
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create("Title", typeof(string), typeof(RegistrationStepViewModel), default(string));

        /// <summary>
        /// The title to display on the page or view rendering this view model.
        /// </summary>
        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// The current step for this view model instance.
        /// </summary>
        public RegistrationStep Step { get; }

        /// <summary>
        /// The <see cref="IUiDispatcher"/> instance.
        /// </summary>
        protected IUiDispatcher UiDispatcher { get; }

        /// <summary>
        /// The <see cref="ITagProfile"/> instance to edit.
        /// </summary>
        protected ITagProfile TagProfile { get; }

        /// <summary>
        /// The <see cref="INeuronService"/> instance.
        /// </summary>
        protected INeuronService NeuronService { get; }

        /// <summary>
        /// The <see cref="INavigationService"/> instance.
        /// </summary>
        protected INavigationService NavigationService { get; }

        /// <summary>
        /// The <see cref="ISettingsService"/> instance.
        /// </summary>
        protected ISettingsService SettingsService { get; }

        /// <summary>
        /// The <see cref="ILogService"/> instance.
        /// </summary>
        protected ILogService LogService { get; }

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
        /// A helper method for asynchronously setting this registration step to Done, and also calling
        /// <see cref="Command.ChangeCanExecute"/> on the list of commands passed in.
        /// </summary>
        /// <param name="commands">The commands to re-evaluate.</param>
        protected void BeginInvokeSetIsDone(params ICommand[] commands)
        {
            UiDispatcher.BeginInvokeOnMainThread(() => SetIsDone(commands));
        }
    }
}