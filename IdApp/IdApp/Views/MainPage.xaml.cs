﻿using IdApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    /// <summary>
    /// A root, or main page, for the application. This is the starting point, from here you can navigate to other pages
    /// and take various actions.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private readonly INeuronService neuronService;

        private static readonly SortedDictionary<string, SortedDictionary<string, string>> ContractTypesPerCategory =
            new SortedDictionary<string, SortedDictionary<string, string>>()
            {/*
                {
                    "Put Title of Contract Category here",
                    new SortedDictionary<string, string>()
                    {
                        { "Put Title of Contract Template here", "Put contract identity of template here." }
                    }
                }*/
            };

        /// <summary>
        /// Creates a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.neuronService = DependencyService.Resolve<INeuronService>();
        }

        /// <inheritdoc />
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.neuronService.ConnectionStateChanged += NeuronService_ConnectionStateChanged;
        }

        /// <inheritdoc />
        protected override void OnDisappearing()
        {
            this.neuronService.ConnectionStateChanged -= NeuronService_ConnectionStateChanged;
            base.OnDisappearing();
        }

        private async void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            const uint durationInMs = 300;
            if (e.IsUserInitiated)
            {
                if (this.neuronService.IsLoggedOut && e.State == XmppState.Offline)
                {
                    // Show (slide down) logout panel
                    await Task.Delay(TimeSpan.FromMilliseconds(durationInMs));
                    this.LogoutPanel.TranslationY = -Height;
                    await this.LogoutPanel.TranslateTo(0, 0, durationInMs, Easing.SinIn);
                }
                else if (!this.neuronService.IsLoggedOut && e.State == XmppState.Connected)
                {
                    // Hide (slide up) logout panel
                    await Task.Delay(TimeSpan.FromMilliseconds(durationInMs));
                    await this.LogoutPanel.TranslateTo(0, -Height, durationInMs, Easing.SinOut);
                }
            }
        }

        private void IdCard_Tapped(object sender, EventArgs e)
        {
            this.IdCard.Flip();
        }
    }
}