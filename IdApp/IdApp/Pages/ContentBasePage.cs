using IdApp.Resx;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Waher.Events;
using System.Threading.Tasks;

namespace IdApp.Pages
{
    /// <summary>
    /// A base class for all pages. This class works in close conjunction with the <see cref="BaseViewModel"/> for binding and unbinding data
    /// when the page is shown on screen.
    /// </summary>
    /// <remarks>It also handles safe area insets for iOS applications, specifically on iPhones with the 'rabbit ear' displays.</remarks>
    public class ContentBasePage : ContentPage
    {
        private const string DefaultMargin = "DefaultMargin";
        private const string SafeAreaInsets = "SafeAreaInsets";
        private const string SafeAreaInsetsDefaultMargin = "SafeAreaInsetsDefaultMargin";

        /// <summary>
        /// Creates an instance of the <see cref="ContentBasePage"/> class.
        /// </summary>
        protected internal ContentBasePage()
        {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == SafeAreaInsets)
            {
                Thickness safeAreaInsets = On<global::Xamarin.Forms.PlatformConfiguration.iOS>().SafeAreaInsets();
                global::Xamarin.Forms.Application.Current.Resources[SafeAreaInsets] = safeAreaInsets;
                Thickness defaultMargin = (Thickness)global::Xamarin.Forms.Application.Current.Resources[DefaultMargin];
                Thickness safeAreaInsetsDefaultMargin = new(defaultMargin.Left + safeAreaInsets.Left, defaultMargin.Top + safeAreaInsets.Top, defaultMargin.Right + safeAreaInsets.Right, defaultMargin.Bottom + safeAreaInsets.Bottom);
                global::Xamarin.Forms.Application.Current.Resources[SafeAreaInsetsDefaultMargin] = safeAreaInsetsDefaultMargin;
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

        /// <inheritdoc />
        protected override sealed async void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                await this.OnAppearingAsync();
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
            }
        }

        /// <summary>
        /// Asynchronous OnAppearing-method.
        /// </summary>
        protected virtual async Task OnAppearingAsync()
        {
            if (!(ViewModel is null))
            {
                if (!ViewModel.IsBound)
                {
                    try
                    {
                        await ViewModel.Bind();
                    }
                    catch (Exception e)
                    {
                        e = Waher.Events.Log.UnnestException(e);
                        this.ViewModel.LogService.LogException(e);
                        string msg = string.Format(AppResources.FailedToBindViewModelForPage, ViewModel.GetType().FullName, this.GetType().FullName);
                        await this.ViewModel.UiSerializer.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                    }
                }

                try
                {
                    if (await this.ViewModel.SettingsService.WaitInitDone())
                        await ViewModel.RestoreState();
                }
                catch (Exception e)
                {
                    e = Waher.Events.Log.UnnestException(e);
                    this.ViewModel.LogService.LogException(e);
                    string msg = string.Format(AppResources.FailedToRestoreViewModelStateForPage, ViewModel.GetType().FullName, this.GetType().FullName);
                    await this.ViewModel.UiSerializer.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                }
            }
        }

        /// <inheritdoc />
        protected override sealed async void OnDisappearing()
        {
            try
            {
                await this.OnDisappearingAsync();
                base.OnDisappearing();
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
            }
        }

        /// <summary>
        /// Asynchronous OnAppearing-method.
        /// </summary>
        protected virtual async Task OnDisappearingAsync()
        {
            if (!(ViewModel is null))
            {
                if (ViewModel.IsBound)
                {
                    try
                    {
                        if (await this.ViewModel.SettingsService.WaitInitDone())
                            await ViewModel.SaveState();
                    }
                    catch (Exception e)
                    {
                        e = Waher.Events.Log.UnnestException(e);
                        this.ViewModel.LogService.LogException(e);
                        string msg = string.Format(AppResources.FailedToSaveViewModelStateForPage, ViewModel.GetType().FullName, this.GetType().FullName);
                        await this.ViewModel.UiSerializer.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                    }
                }

                try
                {
                    await ViewModel.Unbind();
                }
                catch (Exception e)
                {
                    e = Waher.Events.Log.UnnestException(e);
                    this.ViewModel.LogService.LogException(e);
                    string msg = string.Format(AppResources.FailedToUnbindViewModelForPage, ViewModel.GetType().FullName, this.GetType().FullName);
                    await this.ViewModel.UiSerializer.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                }
            }
        }

        /// <summary>
        /// TODO: remove this, as it shouldn't be needed. It's here because ScrollView's typically don't refresh when their child content changes size.
        /// It's a helper method for forcing a layout re-render of the specified components. It will do so asynchronously, by executing a BeginInvoke.
        /// </summary>
        /// <param name="layouts">The layout components to re-render.</param>
        protected void ForceReRender(params Layout[] layouts)
        {
            // Important to BeginInvoke here to get the UI to update properly.
            this.ViewModel.UiSerializer.BeginInvokeOnMainThread(() =>
            {
                foreach (Layout layout in layouts)
                {
                    layout.ForceLayout();
                }
            });
        }
    }
}