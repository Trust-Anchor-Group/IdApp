using System;
using System.ComponentModel;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;

namespace IdApp.Pages.Identity.TransferIdentity
{
    /// <summary>
    /// A page to display when the user wants to view an identity.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class TransferIdentityPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="TransferIdentityPage"/> class.
        /// </summary>
        public TransferIdentityPage()
        {
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new TransferIdentityViewModel(
                App.Instantiate<ITagProfile>(),
                App.Instantiate<IUiSerializer>(),
                App.Instantiate<INeuronService>(),
                this.navigationService ?? App.Instantiate<INavigationService>(),
                App.Instantiate<ILogService>());

            InitializeComponent();
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns>Whether or not the back navigation was handled</returns>
        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}
