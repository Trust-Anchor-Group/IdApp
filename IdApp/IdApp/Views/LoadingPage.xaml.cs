﻿using IdApp.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views
{
    /// <summary>
    /// A page to use when the application is loading, or initializing.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoadingPage
    {
        /// <summary>
        /// Creates a new instance of the <see cref="LoadingPage"/> class.
        /// </summary>
        public LoadingPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            ViewModel = new LoadingViewModel();
            InitializeComponent();
        }

        /// <summary>
        /// Overridden to start an animation when the page is displayed on screen.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LabelLayout.FadeTo(1.0, 2000, Easing.CubicInOut);
        }
    }
}