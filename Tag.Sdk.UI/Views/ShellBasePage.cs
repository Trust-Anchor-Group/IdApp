using System.ComponentModel;
using Xamarin.Forms;
using Tag.Sdk.UI.ViewModels;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Tag.Sdk.UI.Views
{
    public class ShellBasePage : Shell
    {
        private const string DefaultMargin = "DefaultMargin";
        private const string SafeAreaInsets = "SafeAreaInsets";
        private const string SafeAreaInsetsDefaultMargin = "SafeAreaInsetsDefaultMargin";

        public ShellBasePage()
        {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == SafeAreaInsets)
            {
                Thickness safeAreaInsets = On<Xamarin.Forms.PlatformConfiguration.iOS>().SafeAreaInsets();
                Xamarin.Forms.Application.Current.Resources[SafeAreaInsets] = safeAreaInsets;
                Thickness defaultMargin = (Thickness)Xamarin.Forms.Application.Current.Resources[DefaultMargin];
                Thickness safeAreaInsetsDefaultMargin = new Thickness(defaultMargin.Left + safeAreaInsets.Left, defaultMargin.Top + safeAreaInsets.Top, defaultMargin.Right + safeAreaInsets.Right, defaultMargin.Bottom + safeAreaInsets.Bottom);
                Xamarin.Forms.Application.Current.Resources[SafeAreaInsetsDefaultMargin] = safeAreaInsetsDefaultMargin;
            }
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