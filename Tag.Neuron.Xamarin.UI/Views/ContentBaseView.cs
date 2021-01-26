using Tag.Neuron.Xamarin.UI.ViewModels;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Views
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