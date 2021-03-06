﻿using IdApp.ViewModels;
using Tag.Neuron.Xamarin.Services;
using Waher.Runtime.Inventory;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    /// <summary>
    /// The page displaying current XMPP Communication
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class XmppCommunicationPage
    {
        private readonly INavigationService navigationService;
        /// <summary>
        /// Create an instance of a <see cref="XmppCommunicationPage"/>.
        /// </summary>
        public XmppCommunicationPage()
        {
            InitializeComponent();
            this.navigationService = Types.Instantiate<INavigationService>(false);
            this.ViewModel = new XmppCommunicationViewModel();
        }

        /// <summary>
        /// Overrides the back button behavior to handle navigation internally instead.
        /// </summary>
        /// <returns></returns>
        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
    }
}