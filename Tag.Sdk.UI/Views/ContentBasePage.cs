using Xamarin.Forms;
using Tag.Sdk.UI.ViewModels;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Tag.Sdk.UI.Views
{
    public class ContentBasePage : ContentPage
    {
        public ContentBasePage()
        {
            On<iOS>().SetUseSafeArea(true);
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
                    await ViewModel.Bind();
                }
                await ViewModel.RestoreState();
            }
        }

        protected override async void OnDisappearing()
        {
            if (ViewModel != null)
            {
                if (ViewModel.IsBound)
                {
                    await ViewModel.SaveState();
                }
                await ViewModel.Unbind();
            }
            base.OnDisappearing();
        }
    }
}