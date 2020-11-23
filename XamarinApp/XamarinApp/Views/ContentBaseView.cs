using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.ViewModels;

namespace XamarinApp.Views
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

        protected internal async Task OnPageAppearing()
        {
            await this.BindViewModel(ViewModel);
        }

        protected internal async Task OnPageDisappearing()
        {
            await this.UnbindViewModel(ViewModel);
        }
    }
}