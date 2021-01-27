﻿using System;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Tag.Neuron.Xamarin.UI.Views
{
    /// <summary>
    /// A base class for all pages. This class works in close conjunction with the <see cref="BaseViewModel"/> for binding and unbinding data
    /// when the page is shown on screen.
    /// </summary>
    /// <remarks>It also handles safe area insets for iOS applications, specifically on iPhones with the 'rabbit ear' displays.</remarks>
    public class ContentBasePage : ContentPage
    {
        private const string DefaultMargin = "DefaultMargin";
        private const string SafeAreaInsets = "SafeAreaInsets";
        private const string SafeAreaInsetsDefaultMargin = "SafeAreaInsetsDefaultMargin";
        private readonly ILogService logService;
        private readonly IUiDispatcher uiDispatcher;

        public ContentBasePage()
        {
            this.logService = DependencyService.Resolve<ILogService>();
            this.uiDispatcher = DependencyService.Resolve<IUiDispatcher>(); 
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == SafeAreaInsets)
            {
                Thickness safeAreaInsets = On<global::Xamarin.Forms.PlatformConfiguration.iOS>().SafeAreaInsets();
                global::Xamarin.Forms.Application.Current.Resources[SafeAreaInsets] = safeAreaInsets;
                Thickness defaultMargin = (Thickness)global::Xamarin.Forms.Application.Current.Resources[DefaultMargin];
                Thickness safeAreaInsetsDefaultMargin = new Thickness(defaultMargin.Left + safeAreaInsets.Left, defaultMargin.Top + safeAreaInsets.Top, defaultMargin.Right + safeAreaInsets.Right, defaultMargin.Bottom + safeAreaInsets.Bottom);
                global::Xamarin.Forms.Application.Current.Resources[SafeAreaInsetsDefaultMargin] = safeAreaInsetsDefaultMargin;
            }
        }

        /// <summary>
        /// Typed convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
        /// </summary>
        protected BaseViewModel ViewModel
        {
            get => BindingContext as BaseViewModel;
            set => BindingContext = value;
        }

        /// <summary>
        /// Returns the viewmodel, type cast to the proper type.
        /// </summary>
        /// <typeparam name="T">The viewmodel type.</typeparam>
        /// <returns></returns>
        protected T GetViewModel<T>() where T : BaseViewModel
        {
            return (T)ViewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (ViewModel != null)
            {
                if (!ViewModel.IsBound)
                {
                    try
                    {
                        await ViewModel.Bind();
                    }
                    catch (Exception e)
                    {
                        this.logService.LogException(e);
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToBindViewModelForPage, ViewModel.GetType().FullName, this.GetType().FullName));
                    }
                }

                try
                {
                    await ViewModel.RestoreState();
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToRestoreViewModelStateForPage, ViewModel.GetType().FullName, this.GetType().FullName));
                }
            }
        }

        protected override async void OnDisappearing()
        {
            if (ViewModel != null)
            {
                if (ViewModel.IsBound)
                {
                    try
                    {
                        await ViewModel.SaveState();
                    }
                    catch (Exception e)
                    {
                        this.logService.LogException(e);
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToSaveViewModelStateForPage, ViewModel.GetType().FullName, this.GetType().FullName));
                    }
                }

                try
                {
                    await ViewModel.Unbind();
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToUnbindViewModelForPage, ViewModel.GetType().FullName, this.GetType().FullName));
                }
            }
            base.OnDisappearing();
        }
    }
}