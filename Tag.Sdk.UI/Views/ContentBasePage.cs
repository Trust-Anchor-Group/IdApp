using System;
using System.ComponentModel;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;
using Xamarin.Forms;
using Tag.Sdk.UI.ViewModels;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Tag.Sdk.UI.Views
{
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
                Thickness safeAreaInsets = On<Xamarin.Forms.PlatformConfiguration.iOS>().SafeAreaInsets();
                Xamarin.Forms.Application.Current.Resources[SafeAreaInsets] = safeAreaInsets;
                Thickness defaultMargin = (Thickness)Xamarin.Forms.Application.Current.Resources[DefaultMargin];
                Thickness safeAreaInsetsDefaultMargin = new Thickness(defaultMargin.Left + safeAreaInsets.Left, defaultMargin.Top + safeAreaInsets.Top, defaultMargin.Right + safeAreaInsets.Right, defaultMargin.Bottom + safeAreaInsets.Bottom);
                Xamarin.Forms.Application.Current.Resources[SafeAreaInsetsDefaultMargin] = safeAreaInsetsDefaultMargin;
            }
        }

        protected BaseViewModel ViewModel
        {
            get => BindingContext as BaseViewModel;
            set => BindingContext = value;
        }

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