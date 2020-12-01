using Xamarin.Forms;
using XamarinApp.ViewModels;

namespace XamarinApp
{
    public static class AttachedProperties
    {
        public static readonly BindableProperty ViewModelProperty =
            BindableProperty.CreateAttached("ViewModel", typeof(BaseViewModel), typeof(ContentPage), default(BaseViewModel), propertyChanged: OnViewModelChanged);

        static void OnViewModelChanged(BindableObject bindable, object oldValue, object newValue)
        {
            bindable.BindingContext = newValue;
        }

        public static bool GetViewModel(BindableObject bindable)
        {
            return (bool)bindable.GetValue(ViewModelProperty);
        }

        public static void SetViewModel(BindableObject bindable, BaseViewModel value)
        {
            bindable.SetValue(ViewModelProperty, value);
        }
    }
}