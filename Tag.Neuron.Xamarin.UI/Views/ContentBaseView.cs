using Tag.Neuron.Xamarin.UI.ViewModels;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Views
{
    /// <summary>
    /// A convenience base class for <see cref="ContentView"/>s which provides typed access to the viewmodel it is bound to.
    /// </summary>
    public class ContentBaseView : ContentView
    {
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
    }
}