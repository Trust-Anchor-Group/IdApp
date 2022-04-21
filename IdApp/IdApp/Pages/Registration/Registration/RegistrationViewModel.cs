using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.Tag;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Command = Xamarin.Forms.Command;

namespace IdApp.Pages.Registration.Registration
{
    /// <summary>
    /// The view model to bind to for displaying a registration page or view to the user.
    /// </summary>
    public class RegistrationViewModel : BaseViewModel
    {
        private bool muteStepSync;

        /// <summary>
        /// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
        /// </summary>
        protected internal RegistrationViewModel()
        {
            GoToPrevCommand = new Command(GoToPrev, () => (RegistrationStep)CurrentStep > RegistrationStep.ValidatePhoneNr);
            
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>
            {
                this.AddChildViewModel(new ValidatePhoneNr.ValidatePhoneNrViewModel()),
                this.AddChildViewModel(new ChooseAccount.ChooseAccountViewModel()),
                this.AddChildViewModel(new RegisterIdentity.RegisterIdentityViewModel()),
                this.AddChildViewModel(new ValidateIdentity.ValidateIdentityViewModel()),
                this.AddChildViewModel(new DefinePin.DefinePinViewModel())
            };
            
            SyncTagProfileStep();
            UpdateStepTitle();
        }

        /// <inheritdoc />
        protected override async Task DoBind()
        {
            await base.DoBind();
            RegistrationSteps.ForEach(x => x.StepCompleted += RegistrationStep_Completed);
            SyncTagProfileStep();
        }

        /// <inheritdoc />
        protected override Task DoUnbind()
        {
            RegistrationSteps.ForEach(x => x.StepCompleted -= RegistrationStep_Completed);
            return base.DoUnbind();
        }

        #region Properties

        /// <summary>
        /// The list of steps needed to register a digital identity.
        /// </summary>
        public ObservableCollection<RegistrationStepViewModel> RegistrationSteps { get; }

        /// <summary>
        /// The command to bind to for moving backwards to the previous step in the registration process.
        /// </summary>
        public ICommand GoToPrevCommand { get; }

        /// <summary>
        /// See <see cref="CanGoBack"/>
        /// </summary>
        public static readonly BindableProperty CanGoBackProperty =
            BindableProperty.Create(nameof(CanGoBack), typeof(bool), typeof(RegistrationViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether navigation back to the previous registration step can be performed.
        /// </summary>
        public bool CanGoBack
        {
            get { return (bool)this.GetValue(CanGoBackProperty); }
            set { this.SetValue(CanGoBackProperty, value); }
        }

        /// <summary>
        /// See <see cref="CurrentStep"/>
        /// </summary>
        public static readonly BindableProperty CurrentStepProperty =
            BindableProperty.Create(nameof(CurrentStep), typeof(int), typeof(RegistrationViewModel), default(int), propertyChanged: (b, oldValue, newValue) =>
            {
                RegistrationViewModel viewModel = (RegistrationViewModel)b;
                viewModel.UpdateStepTitle();
                viewModel.CanGoBack = viewModel.GoToPrevCommand.CanExecute(null);
            });

        /// <summary>
        /// Gets or sets the current step from the list of <see cref="RegistrationSteps"/>.
        /// </summary>
        public int CurrentStep
        {
            get { return (int)this.GetValue(CurrentStepProperty); }
            set
            {
                if (!this.muteStepSync)
                {
                    this.SetValue(CurrentStepProperty, value);
                }
            }
        }

        /// <summary>
        /// See <see cref="CurrentStep"/>
        /// </summary>
        public static readonly BindableProperty CurrentStepTitleProperty =
            BindableProperty.Create(nameof(CurrentStepTitle), typeof(string), typeof(RegistrationViewModel), default(string));

        /// <summary>
        /// The title of the current step. Displayed in the UI.
        /// </summary>
        public string CurrentStepTitle
        {
            get { return (string)this.GetValue(CurrentStepTitleProperty); }
            set { this.SetValue(CurrentStepTitleProperty, value); }
        }

        #endregion

        /// <summary>
        /// Temporarily mutes the synchronization of the <see cref="CurrentStep"/> property.
        /// This is a hack to workaround a bug on Android.
        /// </summary>
        public void MuteStepSync()
        {
            this.muteStepSync = true;
        }

        /// <summary>
        /// Un-mutes the synchronization of the <see cref="CurrentStep"/> property. See <see cref="MuteStepSync"/>.
        /// This is a hack to workaround a bug on Android.
        /// </summary>
        public void UnMuteStepSync()
        {
            this.muteStepSync = false;
        }

        private void UpdateStepTitle()
        {
            this.CurrentStepTitle = this.RegistrationSteps[this.CurrentStep].Title;
        }

        /// <summary>
        /// An event handler for listening to completion of the different registration steps.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The default event args.</param>
        protected internal void RegistrationStep_Completed(object sender, EventArgs e)
        {
            RegistrationStep step = ((RegistrationStepViewModel)sender).Step;
            switch (step)
            {
                case RegistrationStep.Account:
                    // User connected to an existing account (as opposed to creating a new one). Copy values from the legal identity.
                    if (!(this.TagProfile.LegalIdentity is null))
                    {
                        RegisterIdentity.RegisterIdentityViewModel vm = (RegisterIdentity.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
                        vm.PopulateFromTagProfile();
                    }
                    SyncTagProfileStep();
                    break;

                case RegistrationStep.RegisterIdentity:
                    SyncTagProfileStep();
                    break;

                case RegistrationStep.ValidateIdentity:
                    SyncTagProfileStep();
                    break;

                case RegistrationStep.Pin:
                    this.NavigationService.GoToAsync($"///{nameof(Main.Main.MainPage)}");
                    break;

                default: // RegistrationStep.Operator
                    SyncTagProfileStep();
                    break;
            }
        }

        private void GoToPrev()
        {
            RegistrationStep currStep = (RegistrationStep)CurrentStep;

            switch (currStep)
            {
                case RegistrationStep.Account:
                    this.RegistrationSteps[CurrentStep].ClearStepState();
                    this.TagProfile.ClearAccount();
                    break;

                case RegistrationStep.RegisterIdentity:
                    this.RegistrationSteps[CurrentStep].ClearStepState();
                    this.TagProfile.ClearLegalIdentity();
                    break;

                case RegistrationStep.ValidateIdentity:
                    RegisterIdentity.RegisterIdentityViewModel vm = (RegisterIdentity.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
                    vm.PopulateFromTagProfile();
                    this.RegistrationSteps[CurrentStep].ClearStepState();
                    this.TagProfile.ClearIsValidated();
                    break;

                case RegistrationStep.Pin:
                    this.RegistrationSteps[CurrentStep].ClearStepState();
                    this.TagProfile.ClearPin();
                    break;

                default: // RegistrationStep.Operator
                    this.TagProfile.ClearDomain();
                    break;
            }

            SyncTagProfileStep();
        }

        private void SyncTagProfileStep()
        {
            if (this.TagProfile.Step == RegistrationStep.Complete)
                this.NavigationService.GoToAsync($"///{nameof(Main.Main.MainPage)}");
            else
                this.CurrentStep = (int)this.TagProfile.Step;
        }
    }
}