﻿using IdApp.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Command = Xamarin.Forms.Command;

namespace IdApp.ViewModels.Registration
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
            : this(null, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
        /// For unit tests.
        /// </summary>
        protected internal RegistrationViewModel(
            ITagProfile tagProfile,
            IUiDispatcher uiDispatcher, 
            ISettingsService settingsService, 
            INeuronService neuronService, 
            ICryptoService cryptoService, 
            INavigationService navigationService,
            INetworkService networkService, 
            ILogService logService)
        {
            this.tagProfile = tagProfile ?? DependencyService.Resolve<ITagProfile>();
            uiDispatcher = uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>();
            settingsService = settingsService ?? DependencyService.Resolve<ISettingsService>();
            neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
            cryptoService = cryptoService ?? DependencyService.Resolve<ICryptoService>();
            this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
            networkService = networkService ?? DependencyService.Resolve<INetworkService>();
            logService = logService ?? DependencyService.Resolve<ILogService>();
            GoToPrevCommand = new Command(GoToPrev, () => (RegistrationStep)CurrentStep > RegistrationStep.Operator);
            RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>
            {
                this.AddChildViewModel(new ChooseOperatorViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService, networkService, logService)),
                this.AddChildViewModel(new ChooseAccountViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService, cryptoService, networkService, logService)),
                this.AddChildViewModel(new RegisterIdentityViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService,  networkService, logService)),
                this.AddChildViewModel(new ValidateIdentityViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService, networkService, logService)),
                this.AddChildViewModel(new DefinePinViewModel(this.tagProfile, uiDispatcher, neuronService, this.navigationService, settingsService, logService))
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

        private void RegistrationStep_Completed(object sender, EventArgs e)
        {
            RegistrationStep step = ((RegistrationStepViewModel)sender).Step;
            switch (step)
            {
                case RegistrationStep.Account:
                    // User connected to an existing account (as opposed to creating a new one). Copy values from the legal identity.
                    if (this.tagProfile.LegalIdentity != null)
                    {
                        RegisterIdentityViewModel vm = (RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
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
                    this.navigationService.GoToAsync($"///{nameof(MainPage)}");
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