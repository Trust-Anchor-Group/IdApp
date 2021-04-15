using System;
using System.ComponentModel;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Runtime.Inventory;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace Tag.Neuron.Xamarin.UI.Views
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
        private readonly ILogService logService;
        private readonly ISettingsService settingsService;
        private readonly IUiDispatcher uiDispatcher;

        /// <summary>
        /// Creates an instance of the <see cref="ContentBasePage"/> class.
        /// </summary>
        public ContentBasePage()
            : this(null, null, null)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="ContentBasePage"/> class.
        /// For unit tests.
        /// </summary>
        protected internal ContentBasePage(ILogService logService, ISettingsService settingsService, IUiDispatcher uiDispatcher)
        {
            this.logService = logService ?? Types.Instantiate<ILogService>(false);
            this.settingsService = settingsService ?? Types.Instantiate<ISettingsService>(false);
            this.uiDispatcher = uiDispatcher ?? Types.Instantiate<IUiDispatcher>(false);
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == SafeAreaInsets)
            {
                Thickness safeAreaInsets = On<global::Xamarin.Forms.PlatformConfiguration.iOS>().SafeAreaInsets();
                global::Xamarin.Forms.Application.Current.Resources[SafeAreaInsets] = safeAreaInsets;
                Thickness defaultMargin = (Thickness)global::Xamarin.Forms.Application.Current.Resources[DefaultMargin];
                Thickness safeAreaInsetsDefaultMargin = new Thickness(defaultMargin.Left + safeAreaInsets.Left, defaultMargin.Top + safeAreaInsets.Top, defaultMargin.Right + safeAreaInsets.Right, defaultMargin.Bottom + safeAreaInsets.Bottom);
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
        /// <returns></returns>
        protected T GetViewModel<T>() where T : BaseViewModel
        {
            return (T)ViewModel;
        }

        /// <inheritdoc/>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
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
                        this.logService.LogException(e);
                        string msg = string.Format(AppResources.FailedToBindViewModelForPage, ViewModel.GetType().FullName, this.GetType().FullName);
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                    }
                }

                try
                {
                    if (await this.settingsService.WaitInitDone())
                        await ViewModel.RestoreState();
                }
                catch (Exception e)
                {
                    e = Waher.Events.Log.UnnestException(e);
                    this.logService.LogException(e);
                    string msg = string.Format(AppResources.FailedToRestoreViewModelStateForPage, ViewModel.GetType().FullName, this.GetType().FullName);
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                }
            }
        }

        /// <inheritdoc/>
        protected override async void OnDisappearing()
        {
            if (!(ViewModel is null))
            {
                if (ViewModel.IsBound)
                {
                    try
                    {
                        if (await this.settingsService.WaitInitDone())
                            await ViewModel.SaveState();
                    }
                    catch (Exception e)
                    {
                        e = Waher.Events.Log.UnnestException(e);
                        this.logService.LogException(e);
                        string msg = string.Format(AppResources.FailedToSaveViewModelStateForPage, ViewModel.GetType().FullName, this.GetType().FullName);
                        await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                    }
                }

                try
                {
                    await ViewModel.Unbind();
                }
                catch (Exception e)
                {
                    e = Waher.Events.Log.UnnestException(e);
                    this.logService.LogException(e);
                    string msg = string.Format(AppResources.FailedToUnbindViewModelForPage, ViewModel.GetType().FullName, this.GetType().FullName);
                    await this.uiDispatcher.DisplayAlert(AppResources.ErrorTitle, msg + Environment.NewLine + e.Message);
                }
            }
            base.OnDisappearing();
        }

        /// <summary>
        /// TODO: remove this, as it shouldn't be needed. It's here because ScrollView's typically don't refresh when their child content changes size.
        /// It's a helper method for forcing a layout re-render of the specified components. It will do so asynchronously, by executing a BeginInvoke.
        /// </summary>
        /// <param name="layouts">The layout components to re-render.</param>
        protected void ForceReRender(params Layout[] layouts)
        {
            // Important to BeginInvoke here to get the UI to update properly.
            this.uiDispatcher.BeginInvokeOnMainThread(() =>
            {
                foreach (Layout layout in layouts)
                {
                    layout.ForceLayout();
                }
            });
        }
    }
}