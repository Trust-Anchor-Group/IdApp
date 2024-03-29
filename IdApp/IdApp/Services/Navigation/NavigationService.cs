﻿using IdApp.Pages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Services.Navigation
{
	[Singleton]
	internal sealed partial class NavigationService : LoadableService, INavigationService
	{
		private bool isNavigating = false;
		private readonly Dictionary<string, NavigationArgs> navigationArgsMap = new();

		public NavigationService()
		{
		}

		// Navigation service uses Shell and its routing system, which are only available after the application reached the main page.
		// Before that (during on-boarding and the loading page), we need to use the usual Xamarin Forms navigation.
		private bool CanUseNavigationService => App.IsOnboarded;

		/// <inheritdoc/>
		public Page CurrentPage => Shell.Current?.CurrentPage;

		///<inheritdoc/>
		public override Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			if (this.BeginLoad(cancellationToken))
			{
				try
				{
					Application Application = Application.Current;
					Application.PropertyChanging += this.OnApplicationPropertyChanging;
					Application.PropertyChanged += this.OnApplicationPropertyChanged;
					this.SubscribeToShellNavigatingIfNecessary(Application);

					this.EndLoad(true);
				}
				catch (Exception e)
				{
					this.LogService.LogException(e);
					this.EndLoad(false);
				}
			}

			return Task.CompletedTask;
		}

		///<inheritdoc/>
		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				try
				{
					Application Application = Application.Current;
					this.UnsubscribeFromShellNavigatingIfNecessary(Application);
					Application.PropertyChanged -= this.OnApplicationPropertyChanged;
					Application.PropertyChanging -= this.OnApplicationPropertyChanging;
				}
				catch (Exception e)
				{
					this.LogService.LogException(e);
				}

				this.EndUnload();
			}

			return Task.CompletedTask;
		}


		/// <inheritdoc/>
		public Task GoToAsync(string Route, BackMethod BackMethod = BackMethod.Inherited, string UniqueId = null)
		{
			// No args navigation will create a defaul navigation arguments
			return this.GoToAsync<NavigationArgs>(Route, null, BackMethod, UniqueId);
		}

		/// <inheritdoc/>
		public async Task GoToAsync<TArgs>(string Route, TArgs Args, BackMethod BackMethod = BackMethod.Inherited, string UniqueId = null) where TArgs : NavigationArgs, new()
		{
			if (!this.CanUseNavigationService)
				return;

			await this.UiSerializer.InvokeOnMainThreadAsync(async () =>
			{
				// Get the parent's navigation arguments
				NavigationArgs ParentArgs = this.GetCurrentNavigationArgs();

				// Create a default navigation arguments if Args are null
				NavigationArgs NavigationArgs = Args ?? new();

				NavigationArgs.SetBackArguments(BackMethod, ParentArgs, UniqueId);
				this.PushArgs(Route, NavigationArgs);

				if (!string.IsNullOrEmpty(UniqueId))
				{
					Route += "?UniqueId=" + UniqueId;
				}

				try
				{
					this.isNavigating = true;
					await Shell.Current.GoToAsync(Route, true);
				}
				catch (Exception e)
				{
					e = Log.UnnestException(e);
					this.LogService.LogException(e);
					string ExtraInfo = Environment.NewLine + e.Message;
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["FailedToNavigateToPage"], Route, ExtraInfo));
				}
				finally
				{
					this.isNavigating = false;
				}
			});
		}

		///<inheritdoc/>
		public async Task GoBackAsync(bool Animate = true)
		{
			if (!this.CanUseNavigationService)
				return;

			try
			{
				NavigationArgs NavigationArgs = this.GetCurrentNavigationArgs();

				if (NavigationArgs is not null) // the main view?
				{
					string BackRoute = NavigationArgs.GetBackRoute();

					this.isNavigating = true;
					await Shell.Current.GoToAsync(BackRoute, Animate);
				}
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["FailedToClosePage"]);
			}
			finally
			{
				this.isNavigating = false;
			}
		}

		///<inheritdoc/>
		public bool TryGetArgs<TArgs>(out TArgs Args, string UniqueId = null) where TArgs : NavigationArgs
		{
			NavigationArgs NavigationArgs = null;

			if (this.CanUseNavigationService && (this.CurrentPage is Page Page))
			{
				NavigationArgs = this.TryGetArgs(Page.GetType().Name, UniqueId);
				string Route = Routing.GetRoute(Page);
				NavigationArgs ??= this.TryGetArgs(Route, UniqueId);

				if ((NavigationArgs is null) && (UniqueId is null) &&
					(Page is ContentBasePage BasePage) && (BasePage.UniqueId is not null))
				{
					return this.TryGetArgs(out Args, BasePage.UniqueId);
				}
			}

			Args = NavigationArgs as TArgs;

			return (Args is not null);
		}

		private NavigationArgs GetCurrentNavigationArgs()
		{
			this.TryGetArgs(out NavigationArgs Args);
			return Args;
		}

		private void OnApplicationPropertyChanged(object Sender, System.ComponentModel.PropertyChangedEventArgs Args)
		{
			if (Args.PropertyName == nameof(Application.MainPage))
				this.SubscribeToShellNavigatingIfNecessary((Application)Sender);
		}

		private void OnApplicationPropertyChanging(object Sender, PropertyChangingEventArgs Args)
		{
			if (Args.PropertyName == nameof(Application.MainPage))
				this.UnsubscribeFromShellNavigatingIfNecessary((Application)Sender);
		}

		private void SubscribeToShellNavigatingIfNecessary(Application Application)
		{
			if (Application.MainPage is Shell Shell)
				Shell.Navigating += this.Shell_Navigating;
		}

		private void UnsubscribeFromShellNavigatingIfNecessary(Application Application)
		{
			if (Application.MainPage is Shell Shell)
				Shell.Navigating -= this.Shell_Navigating;
		}

		private void Shell_Navigating(object Sender, ShellNavigatingEventArgs e)
		{
			try
			{
				if ((e.Source == ShellNavigationSource.Pop) &&
					e.CanCancel && !this.isNavigating)
				{
					e.Cancel();

					this.UiSerializer.BeginInvokeOnMainThread(async () =>
					{
						await this.GoBackAsync();
					});
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private bool TryGetPageName(string Route, out string PageName)
		{
			PageName = null;

			if (!string.IsNullOrWhiteSpace(Route))
			{
				PageName = Route.TrimStart('.', '/');
				return !string.IsNullOrWhiteSpace(PageName);
			}

			return false;
		}

		private void PushArgs(string Route, NavigationArgs Args)
		{
			if (this.TryGetPageName(Route, out string PageName))
			{
				if (Args is not null)
				{
					string UniqueId = Args.GetUniqueId();
					if (!string.IsNullOrEmpty(UniqueId))
						PageName += UniqueId;

					this.navigationArgsMap[PageName] = Args;
				}
				else
					this.navigationArgsMap.Remove(PageName);
			}
		}

		private NavigationArgs TryGetArgs(string Route, string UniqueId)
		{
			if (!string.IsNullOrEmpty(UniqueId))
				Route += UniqueId;

			if (this.TryGetPageName(Route, out string PageName) &&
				this.navigationArgsMap.TryGetValue(PageName, out NavigationArgs Args))
			{
				return Args;
			}

			return null;
		}
	}
}
