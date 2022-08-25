using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Waher.Events;
using IdApp.Pages.Registration.Registration;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages
{
    /// <summary>
    /// A base class for all pages. This class works in close conjunction with the <see cref="BaseViewModel"/> for binding and unbinding data
    /// when the page is shown on screen.
    /// </summary>
    /// <remarks>It also handles safe area insets for iOS applications, specifically on iPhones with the 'rabbit ear' displays.</remarks>
    public class ContentBasePage : ContentPage
    {
		/// <summary>
		/// Creates an instance of the <see cref="ContentBasePage"/> class.
		/// </summary>
		protected internal ContentBasePage()
        {
#if DEBUG
			NavigationLogger.Log(this.GetType().Name + " .ctor");
#endif
			this.On<iOS>().SetUseSafeArea(true);
        }

        /// <summary>
        /// Typed convenience property for accessing the <see cref="BindableObject.BindingContext"/> property as a view model.
        /// </summary>
        public BaseViewModel ViewModel
        {
            get => this.BindingContext as BaseViewModel;
            protected set => this.BindingContext = value;
        }

        /// <inheritdoc />
        protected override sealed async void OnAppearing()
        {
            try
            {
                base.OnAppearing();

#if DEBUG
				NavigationLogger.Log(this.GetType().Name + " OnAppearingAsync begins.");
#endif
				await this.OnAppearingAsync();
#if DEBUG
				NavigationLogger.Log(this.GetType().Name + " OnAppearingAsync ends.");
#endif
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
                if (!this.ViewModel.IsAppearing)
                {
                    try
                    {
#if DEBUG
						NavigationLogger.Log(this.GetType().Name + " ViewModel (" + this.ViewModel.GetType().Name + ") Bind begins.");
#endif
						await this.ViewModel.Appearing();
#if DEBUG
						NavigationLogger.Log(this.GetType().Name + " ViewModel (" + this.ViewModel.GetType().Name + ") Bind ends.");
#endif
					}
                    catch (Exception e)
                    {
                        e = Log.UnnestException(e);
                        this.ViewModel.LogService.LogException(e);
                        string msg = string.Format(LocalizationResourceManager.Current["FailedToBindViewModelForPage"], this.ViewModel.GetType().FullName, this.GetType().FullName);
                        await this.ViewModel.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], msg + Environment.NewLine + e.Message);
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
                    string msg = string.Format(LocalizationResourceManager.Current["FailedToRestoreViewModelStateForPage"], this.ViewModel.GetType().FullName, this.GetType().FullName);
                    await this.ViewModel.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], msg + Environment.NewLine + e.Message);
                }
            }
        }

        /// <inheritdoc />
        protected override sealed async void OnDisappearing()
        {
            try
            {
#if DEBUG
				NavigationLogger.Log(this.GetType().Name + " OnDisappearingAsync begins.");
#endif
				await this.OnDisappearingAsync();
#if DEBUG
				NavigationLogger.Log(this.GetType().Name + " OnDisappearingAsync ends.");
#endif
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
                if (this.ViewModel.IsAppearing)
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
                        string msg = string.Format(LocalizationResourceManager.Current["FailedToSaveViewModelStateForPage"], this.ViewModel.GetType().FullName, this.GetType().FullName);
                        await this.ViewModel.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], msg + Environment.NewLine + e.Message);
                    }
                }

                try
                {
#if DEBUG
					NavigationLogger.Log(this.GetType().Name + " ViewModel (" + this.ViewModel.GetType().Name + ") Unbind begins.");
#endif
					await this.ViewModel.Disappearing();
#if DEBUG
					NavigationLogger.Log(this.GetType().Name + " ViewModel (" + this.ViewModel.GetType().Name + ") Unbind ends.");
#endif
				}
                catch (Exception e)
                {
                    e = Log.UnnestException(e);
                    this.ViewModel.LogService.LogException(e);
                    string msg = string.Format(LocalizationResourceManager.Current["FailedToUnbindViewModelForPage"], this.ViewModel.GetType().FullName, this.GetType().FullName);
                    await this.ViewModel.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], msg + Environment.NewLine + e.Message);
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

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled</returns>
		protected sealed override bool OnBackButtonPressed()
		{
			if (this.ViewModel is RegistrationViewModel RegistrationViewModel)
			{
				if (RegistrationViewModel.CanGoBack)
				{
					RegistrationViewModel.GoToPrevCommand.Execute(null);
					return true;
				}
				else
					return base.OnBackButtonPressed();
			}
			else
			{
				if (this.ViewModel is not null)
					this.ViewModel.NavigationService.GoBackAsync();

				return true;
			}
		}

		/// <summary>
		/// Called when the <see cref="Xamarin.Forms.Page"/>'s <see cref="Xamarin.Forms.Element.Parent"/> property has changed.
		/// </summary>
		protected override sealed async void OnParentSet()
		{
			try
			{
				base.OnParentSet();

				if (this.Parent is null)
				{
					if (this.ViewModel is ILifeCycleView LifeCycleView)
						await LifeCycleView.Dispose();
				}
				else
				{
					if (this.ViewModel is ILifeCycleView LifeCycleView)
						await LifeCycleView.Initialize();
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

	}
}
