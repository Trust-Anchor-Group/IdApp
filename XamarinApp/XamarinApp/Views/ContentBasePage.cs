using Xamarin.Forms;
using XamarinApp.ViewModels;

namespace XamarinApp.Views
{
    public class ContentBasePage : ContentPage
    {
        public ContentBasePage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
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