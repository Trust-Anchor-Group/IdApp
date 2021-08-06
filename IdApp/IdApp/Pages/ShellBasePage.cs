using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Application = Xamarin.Forms.Application;

namespace IdApp.Pages
{
    /// <summary>
    /// A base class for <see cref="Shell"/> pages. This class provides easy, typed, access to the <see cref="BindableObject.BindingContext"/> as a view model.
    /// It also handles safe area insets for iOS when the phone has a 'rabbit ear' display.
    /// </summary>
    public abstract class ShellBasePage : Shell
    {
        private const string DefaultMargin = "DefaultMargin";
        private const string SafeAreaInsets = "SafeAreaInsets";
        private const string SafeAreaInsetsDefaultMargin = "SafeAreaInsetsDefaultMargin";

        /// <summary>
        /// Creates an instance of the <see cref="ShellBasePage"/> class.
        /// </summary>
        public ShellBasePage()
        {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == SafeAreaInsets)
            {
                Thickness safeAreaInsets = On<global::Xamarin.Forms.PlatformConfiguration.iOS>().SafeAreaInsets();
                if (Application.Current.Resources.ContainsKey(SafeAreaInsets))
                {
                    Application.Current.Resources[SafeAreaInsets] = safeAreaInsets;
                    Thickness defaultMargin = (Thickness)Application.Current.Resources[DefaultMargin];
                    Thickness safeAreaInsetsDefaultMargin = new Thickness(defaultMargin.Left + safeAreaInsets.Left, defaultMargin.Top + safeAreaInsets.Top, defaultMargin.Right + safeAreaInsets.Right, defaultMargin.Bottom + safeAreaInsets.Bottom);
                    Application.Current.Resources[SafeAreaInsetsDefaultMargin] = safeAreaInsetsDefaultMargin;
                }
            }
        }

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
        /// <returns>View model</returns>
        protected T GetViewModel<T>() where T : BaseViewModel
        {
            return (T)ViewModel;
        }

        /// Due to a bug in Xamarin Forms (https://github.com/xamarin/xamarin.forms/issues/6486)
        /// these two methods aren't called at startup for now. But we're leaving the methods here for the future.

        /// <inheritdoc/>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!(ViewModel is null))
            {
                if (!ViewModel.IsBound)
                {
                    await ViewModel.Bind();
                }
                await ViewModel.RestoreState();
            }
        }

        /// <inheritdoc/>
        protected override async void OnDisappearing()
        {
            if (!(ViewModel is null))
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