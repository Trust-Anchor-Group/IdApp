using System;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.ViewModels;

namespace XamarinApp.Views
{
    public class ContentBasePage : ContentPage
    {
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
            await this.BindViewModel(ViewModel);
            await ViewModel.RestoreState();
        }

        protected override async void OnDisappearing()
        {
            if (ViewModel.IsBound)
            {
                await ViewModel.SaveState();
            }
            await this.UnbindViewModel(ViewModel);
            (ViewModel as IDisposable)?.Dispose();
            base.OnDisappearing();
        }
    }
}