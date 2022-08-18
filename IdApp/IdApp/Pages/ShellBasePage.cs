using System;
using Waher.Events;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace IdApp.Pages
{
    /// <summary>
    /// A base class for <see cref="Shell"/> pages. This class provides easy, typed, access to the <see cref="BindableObject.BindingContext"/> as a view model.
    /// It also handles safe area insets for iOS when the phone has a 'rabbit ear' display.
    /// </summary>
    public abstract class ShellBasePage : Shell
    {
        /// <summary>
        /// Creates an instance of the <see cref="ShellBasePage"/> class.
        /// </summary>
        public ShellBasePage()
        {
			this.On<iOS>().SetUseSafeArea(true);
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

        /// Due to a bug in Xamarin Forms (https://github.com/xamarin/xamarin.forms/issues/6486)
        /// these two methods aren't called at startup for now. But we're leaving the methods here for the future.

        /// <inheritdoc/>
        protected override async void OnAppearing()
        {
			try
			{
				base.OnAppearing();

				if (!(this.ViewModel is null))
				{
					if (!this.ViewModel.IsAppearing)
						await this.ViewModel.Appearing();

					await this.ViewModel.RestoreState();
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
        }

        /// <inheritdoc/>
        protected override async void OnDisappearing()
        {
			try
			{
				if (!(this.ViewModel is null))
				{
					if (this.ViewModel.IsAppearing)
						await this.ViewModel.SaveState();

					await this.ViewModel.Disappearing();
				}

				base.OnDisappearing();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
        }
    }
}
