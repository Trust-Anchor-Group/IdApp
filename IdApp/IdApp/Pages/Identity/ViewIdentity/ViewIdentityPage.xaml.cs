using System;
using System.ComponentModel;
using IdApp.Services.AttachmentCache;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using IdApp.Services.Wallet;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Identity.ViewIdentity
{
    /// <summary>
    /// A page to display when the user wants to view an identity.
    /// </summary>
    [DesignTimeVisible(true)]
    public partial class ViewIdentityPage
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Creates a new instance of the <see cref="ViewIdentityPage"/> class.
        /// </summary>
        public ViewIdentityPage()
        {
            this.navigationService = App.Instantiate<INavigationService>();
            this.ViewModel = new ViewIdentityViewModel(
                App.Instantiate<ITagProfile>(),
                App.Instantiate<IUiDispatcher>(),
                App.Instantiate<INeuronService>(),
                this.navigationService ?? App.Instantiate<INavigationService>(),
                App.Instantiate<INetworkService>(),
                App.Instantiate<ILogService>(),
                App.Instantiate<IEDalerOrchestratorService>(),
                App.Instantiate<IAttachmentCacheService>());
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void OnDisappearing()
        {
            this.PhotoViewer.HidePhotos();
            base.OnDisappearing();
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

        private void Image_Tapped(object sender, EventArgs e)
        {
            Attachment[] attachments = this.GetViewModel<ViewIdentityViewModel>().LegalIdentity?.Attachments;
            this.PhotoViewer.ShowPhotos(attachments);
        }
    }
}
