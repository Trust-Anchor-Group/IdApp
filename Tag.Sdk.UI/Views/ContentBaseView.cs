using Xamarin.Forms;
using Tag.Sdk.UI.ViewModels;

namespace Tag.Sdk.UI.Views
{
    public class ContentBaseView : ContentView
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
    }
}