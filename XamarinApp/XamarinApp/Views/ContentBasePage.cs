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
        }

        protected override async void OnDisappearing()
        {
            await this.UnbindViewModel(ViewModel);
            base.OnDisappearing();
        }
    }
}