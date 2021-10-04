using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.AttachmentCache;
using IdApp.Services.Crypto;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Settings;
using IdApp.Services.Tag;
using IdApp.Services.UI;
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
        private readonly ITagProfile tagProfile;
        private readonly INavigationService navigationService;
        private bool muteStepSync;

        /// <summary>
        /// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
        /// </summary>
        public RegistrationViewModel()
            : this(null, null, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
        /// For unit tests.
        /// <param name="tagProfile">The tag profile to work with.</param>
        /// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
        /// <param name="settingsService">The settings service for persisting UI state.</param>
        /// <param name="neuronService">The Neuron service for XMPP communication.</param>
        /// <param name="cryptoService">The service to use for cryptographic operations.</param>
        /// <param name="navigationService">The navigation service to use for app navigation</param>
        /// <param name="networkService">The network and connectivity service.</param>
        /// <param name="logService">The log service.</param>
        /// <param name="attachmentCacheService">The attachment cache to use.</param>
        /// </summary>
        protected internal RegistrationViewModel(
            ITagProfile tagProfile,
            IUiDispatcher uiDispatcher, 
            ISettingsService settingsService, 
            INeuronService neuronService, 
            ICryptoService cryptoService, 
            INavigationService navigationService,
            INetworkService networkService, 
            ILogService logService,
            IAttachmentCacheService attachmentCacheService)
        {
            this.tagProfile = tagProfile ?? App.Instantiate<ITagProfile>();
            uiDispatcher = uiDispatcher ?? App.Instantiate<IUiDispatcher>();
            settingsService = settingsService ?? App.Instantiate<ISettingsService>();
            neuronService = neuronService ?? App.Instantiate<INeuronService>();
            cryptoService = cryptoService ?? App.Instantiate<ICryptoService>();
            this.navigationService = navigationService ?? App.Instantiate<INavigationService>();
            networkService = networkService ?? App.Instantiate<INetworkService>();
            logService = logService ?? App.Instantiate<ILogService>();
            
            GoToPrevCommand = new Command(GoToPrev, () => (RegistrationStep)CurrentStep > RegistrationStep.ValidatePhoneNr);
            
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>
            {
                this.AddChildViewModel(new ValidatePhoneNr.ValidatePhoneNrViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService, networkService, logService)),
                this.AddChildViewModel(new ChooseAccount.ChooseAccountViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService, cryptoService, networkService, logService)),
                this.AddChildViewModel(new RegisterIdentity.RegisterIdentityViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService,  networkService, logService, attachmentCacheService)),
                this.AddChildViewModel(new ValidateIdentity.ValidateIdentityViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService, networkService, logService, attachmentCacheService)),
                this.AddChildViewModel(new DefinePin.DefinePinViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService, logService))
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
            BindableProperty.Create("CanGoBack", typeof(bool), typeof(RegistrationViewModel), default(bool));

        /// <summary>
        /// Gets or sets whether navigation back to the previous registration step can be performed.
        /// </summary>
        public bool CanGoBack
        {
            get { return (bool) GetValue(CanGoBackProperty); }
            set { SetValue(CanGoBackProperty, value); }
        }

        /// <summary>
        /// See <see cref="CurrentStep"/>
        /// </summary>
        public static readonly BindableProperty CurrentStepProperty =
            BindableProperty.Create("CurrentStep", typeof(int), typeof(RegistrationViewModel), default(int), propertyChanged: (b, oldValue, newValue) =>
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
            get { return (int)GetValue(CurrentStepProperty); }
            set
            {
                if (!this.muteStepSync)
                {
                    SetValue(CurrentStepProperty, value);
                }
            }
        }

        /// <summary>
        /// See <see cref="CurrentStep"/>
        /// </summary>
        public static readonly BindableProperty CurrentStepTitleProperty =
            BindableProperty.Create("CurrentStepTitle", typeof(string), typeof(RegistrationViewModel), default(string));

        /// <summary>
        /// The title of the current step. Displayed in the UI.
        /// </summary>
        public string CurrentStepTitle
        {
            get { return (string) GetValue(CurrentStepTitleProperty); }
            set { SetValue(CurrentStepTitleProperty, value); }
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
                    if (!(this.tagProfile.LegalIdentity is null))
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
                    this.navigationService.GoToAsync($"///{nameof(Main.Main.MainPage)}");
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
                    this.tagProfile.ClearAccount();
                    break;

                case RegistrationStep.RegisterIdentity:
                    this.RegistrationSteps[CurrentStep].ClearStepState();
                    this.tagProfile.ClearLegalIdentity();
                    break;

                case RegistrationStep.ValidateIdentity:
                    RegisterIdentity.RegisterIdentityViewModel vm = (RegisterIdentity.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
                    vm.PopulateFromTagProfile();
                    this.RegistrationSteps[CurrentStep].ClearStepState();
                    this.tagProfile.ClearIsValidated();
                    break;

                case RegistrationStep.Pin:
                    this.RegistrationSteps[CurrentStep].ClearStepState();
                    this.tagProfile.ClearPin();
                    break;

                default: // RegistrationStep.Operator
                    this.tagProfile.ClearDomain();
                    break;
            }

            SyncTagProfileStep();
        }

        private void SyncTagProfileStep()
        {
            if (this.tagProfile.Step != RegistrationStep.Complete)
            {
                this.CurrentStep = (int)this.tagProfile.Step;
            }
        }
    }
}