using Xamarin.Forms;
using XamarinApp.Extensions;
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
            await this.BindViewModel(ViewModel);
            await ViewModel.RestoreState();
            foreach(ContentBaseView view in this.GetChildren<ContentBaseView>())
            {
                await view.OnPageAppearing();
            }
        }

        protected override async void OnDisappearing()
        {
            if (ViewModel.IsBound)
            {
                await ViewModel.SaveState();
            }
            foreach (ContentBaseView view in this.GetChildren<ContentBaseView>())
            {
                await view.OnPageDisappearing();
            }
            await this.UnbindViewModel(ViewModel);
            base.OnDisappearing();
        }
    }
}