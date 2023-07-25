using Xamarin.Forms;

namespace IdApp.Pages
{
    /// <summary>
    /// A convenience base class for <see cref="ContentView"/>s which provides typed access to the viewmodel it is bound to.
    /// </summary>
    public abstract class ContentBaseView : ContentView
    {
        /// <summary>
        /// Typed convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
        /// </summary>
        protected BaseViewModel ContentViewModel
		{
            get => this.BindingContext as BaseViewModel;
            set => this.BindingContext = value;
        }

        /// <summary>
        /// Returns the viewmodel, type cast to the proper type.
        /// </summary>
        /// <typeparam name="T">The viewmodel type.</typeparam>
        /// <returns>View model</returns>
        protected T GetContentViewModel<T>() where T : BaseViewModel
        {
            return (T)this.ContentViewModel;
        }
    }
}
