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
        private const string defaultMargin = "DefaultMargin";
        private const string safeAreaInsets = "SafeAreaInsets";
        private const string safeAreaInsetsDefaultMargin = "SafeAreaInsetsDefaultMargin";

        /// <summary>
        /// Creates an instance of the <see cref="ContentBasePage"/> class.
        /// </summary>
        protected internal ContentBasePage()
        {
            PropertyChanged += this.OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == safeAreaInsets)
            {
                Thickness SafeAreaInsets = this.On<global::Xamarin.Forms.PlatformConfiguration.iOS>().SafeAreaInsets();
                global::Xamarin.Forms.Application.Current.Resources[safeAreaInsets] = safeAreaInsets;
                Thickness DefaultMargin = (Thickness)global::Xamarin.Forms.Application.Current.Resources[defaultMargin];
                Thickness SafeAreaInsetsDefaultMargin = new(DefaultMargin.Left + SafeAreaInsets.Left, DefaultMargin.Top + SafeAreaInsets.Top,
					DefaultMargin.Right + SafeAreaInsets.Right, DefaultMargin.Bottom + SafeAreaInsets.Bottom);
                global::Xamarin.Forms.Application.Current.Resources[safeAreaInsetsDefaultMargin] = SafeAreaInsetsDefaultMargin;
            }
        }

        /// <summary>
        /// Typed convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
        /// </summary>
        protected BaseViewModel ViewModel
        {
            get => this.BindingContext as BaseViewModel;
            set => this.BindingContext = value;
        }

        /// <summary>
        /// Returns the viewmodel, type cast to the proper type.
        /// </summary>
        /// <typeparam name="T">The viewmodel type.</typeparam>
        /// <returns>View model</returns>
        protected T GetViewModel<T>() where T : BaseViewModel
        {
            return (T)this.ViewModel;
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
            if (this.ViewModel is not null)
            {
                if (!this.ViewModel.IsBound)
                {
                    try
                    {
                        await this.ViewModel.Bind();
                    }
                    catch (Exception e)
                    {
                        e = Log.UnnestException(e);
                        this.ViewModel.LogService.LogException(e);
                        string msg = string.Format(AppResources.FailedToBindViewModelForPage, this.ViewModel.GetType().FullName, this.GetType().FullName);
                        await this.ViewModel.UiSerializer.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                    }
                }

                try
                {
                    if (await this.ViewModel.SettingsService.WaitInitDone())
                        await this.ViewModel.RestoreState();
                }
                catch (Exception e)
                {
                    e = Log.UnnestException(e);
                    this.ViewModel.LogService.LogException(e);
                    string msg = string.Format(AppResources.FailedToRestoreViewModelStateForPage, this.ViewModel.GetType().FullName, this.GetType().FullName);
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
            if (this.ViewModel is not null)
            {
                if (this.ViewModel.IsBound)
                {
                    try
                    {
                        if (await this.ViewModel.SettingsService.WaitInitDone())
                            await this.ViewModel.SaveState();
                    }
                    catch (Exception e)
                    {
                        e = Log.UnnestException(e);
                        this.ViewModel.LogService.LogException(e);
                        string msg = string.Format(AppResources.FailedToSaveViewModelStateForPage, this.ViewModel.GetType().FullName, this.GetType().FullName);
                        await this.ViewModel.UiSerializer.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                    }
                }

                try
                {
                    await this.ViewModel.Unbind();
                }
                catch (Exception e)
                {
                    e = Log.UnnestException(e);
                    this.ViewModel.LogService.LogException(e);
                    string msg = string.Format(AppResources.FailedToUnbindViewModelForPage, this.ViewModel.GetType().FullName, this.GetType().FullName);
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
