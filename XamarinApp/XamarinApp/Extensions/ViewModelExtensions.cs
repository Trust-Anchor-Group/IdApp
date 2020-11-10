using XamarinApp.ViewModels;

namespace XamarinApp.Extensions
{
    public static class ViewModelExtensions
    {
        public static T As<T>(this BaseViewModel viewModel) where T : class
        {
            T t = viewModel as T;
            return t;
        }
    }
}