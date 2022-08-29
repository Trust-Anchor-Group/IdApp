using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;
using IdApp.Pages;

namespace IdApp.Services.Navigation
{
	[Singleton]
	internal sealed class NavigationService : LoadableService, INavigationService
	{
		private const string defaultGoBackRoute = "..";
		private readonly Dictionary<string, NavigationArgs> navigationArgsMap;
		private bool isManuallyNavigatingBack = false;

		public NavigationService()
		{
			this.navigationArgsMap = new Dictionary<string, NavigationArgs>();
		}

		// Navigation service uses Shell and its routing system, which are only available after the application reached the main page.
		// Before that (during on-boarding and the loading page), we need to use the usual Xamarin Forms navigation.
		private bool CanUseNavigationService => App.IsOnboarded;


		private NavigationArgs CurrentNavigationArgs =>
			Shell.Current?.CurrentPage is ContentBasePage ContentBasePage && this.TryPopArgs(out NavigationArgs NavigationArgs, ContentBasePage.UniqueId)
			? NavigationArgs : null;

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

		private void OnApplicationPropertyChanged(object Sender, System.ComponentModel.PropertyChangedEventArgs Args)
		{
			if (Args.PropertyName == nameof(Application.MainPage))
			{
				this.SubscribeToShellNavigatingIfNecessary((Application)Sender);
			}
		}

		private void OnApplicationPropertyChanging(object Sender, PropertyChangingEventArgs Args)
		{
			if (Args.PropertyName == nameof(Application.MainPage))
			{
				this.UnsubscribeFromShellNavigatingIfNecessary((Application)Sender);
			}
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

		private async void Shell_Navigating(object Sender, ShellNavigatingEventArgs e)
		{
			try
			{
				if ((e.Source == ShellNavigationSource.Pop) &&
					(this.CurrentNavigationArgs is not null) &&
					(!string.IsNullOrWhiteSpace(this.CurrentNavigationArgs.ReturnRoute) ||
					this.CurrentNavigationArgs.ReturnCounter > 1))
				{
					if (e.CanCancel && !this.isManuallyNavigatingBack)
					{
						this.isManuallyNavigatingBack = true;
						e.Cancel();
						await this.GoBackAsync();
						this.isManuallyNavigatingBack = false;
					}
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

		private void PushArgs<TArgs>(string Route, TArgs args) where TArgs : NavigationArgs
		{
			if (this.TryGetPageName(Route, out string PageName))
			{
				if (args is not null)
				{
					if (!string.IsNullOrEmpty(args.UniqueId))
					{
						PageName += args.UniqueId;
					}

					this.navigationArgsMap[PageName] = args;
				}
				else
					this.navigationArgsMap.Remove(PageName);
			}
		}

		public bool TryPopArgs<TArgs>(out TArgs Args, string UniqueId = null) where TArgs : NavigationArgs
		{
			if (!this.CanUseNavigationService)
			{
				Args = default;
				return false;
			}

			return this.TryPopArgs(Shell.Current.CurrentPage, out Args, UniqueId);
		}

		public TArgs GetPopArgs<TArgs>(string UniqueId = null) where TArgs : NavigationArgs
		{
			return (this.CanUseNavigationService && this.TryPopArgs(Shell.Current.CurrentPage, out TArgs Args, UniqueId)) ? Args : null;
		}

		private bool TryPopArgs<TArgs>(Page CurrentPage, out TArgs Args, string UniqueId = null) where TArgs : NavigationArgs
		{
			if (CurrentPage is null)
			{
				Args = default;
				return false;
			}

			return this.TryPopArgs(CurrentPage.GetType().Name, out Args, UniqueId)
				|| this.TryPopArgs(Routing.GetRoute(CurrentPage), out Args, UniqueId);
		}

		private bool TryPopArgs<TArgs>(string PageName, out TArgs args, string UniqueId = null) where TArgs : NavigationArgs
		{
			args = default;

			if (!string.IsNullOrEmpty(UniqueId))
			{
				PageName += UniqueId;
			}

			if (this.TryGetPageName(PageName, out string pageName) &&
				this.navigationArgsMap.TryGetValue(pageName, out NavigationArgs navArgs) &&
				(navArgs is not null))
			{
				args = navArgs as TArgs;
			}

			return args is not null;
		}

		public Task GoBackAsync()
		{
			return this.GoBackAsync(true);
		}

		public Task GoBackAsync(bool Animate)
		{
#if DEBUG
			NavigationLogger.Log("GoBackAsync.");
#endif

			if (!this.CanUseNavigationService)
				return Task.CompletedTask;

			try
			{
				string ReturnRoute = defaultGoBackRoute;
				int ReturnCounter = 0;

				if (this.CurrentNavigationArgs is not null)
				{
					ReturnCounter = this.CurrentNavigationArgs.ReturnCounter;

					if ((ReturnCounter == 0) &&
						!string.IsNullOrEmpty(this.CurrentNavigationArgs.ReturnRoute))
					{
						ReturnRoute = this.CurrentNavigationArgs.ReturnRoute;
					}
				}

				if (ReturnCounter > 1)
				{
					for (int i = 1; i < ReturnCounter; i++)
					{
						ReturnRoute += "/" + defaultGoBackRoute;
					}
				}

				return Shell.Current.GoToAsync(ReturnRoute, Animate);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
				return this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["FailedToClosePage"]);
			}
		}

		public Task GoToAsync(string Route)
		{
			return this.GoToAsync(Route, (NavigationArgs)null, (NavigationArgs)null);
		}

		public Task GoToAsync<TArgs>(string Route, TArgs args) where TArgs : NavigationArgs, new()
		{
			if (!this.CanUseNavigationService)
				return Task.CompletedTask;

			NavigationArgs NavigationArgs = this.GetPopArgs<NavigationArgs>();

			if ((args is not null) && args.CancelReturnCounter)
			{
				// ignore the previous args if the return counter was canceled
				NavigationArgs = null;
			}

			return this.GoToAsync(Route, args, NavigationArgs);
		}

		private Task GoToAsync<TArgs>(string Route, TArgs args, NavigationArgs NavigationArgs) where TArgs : NavigationArgs, new()
		{
#if DEBUG
			NavigationLogger.Log("Navigating to " + (Route ?? "(null)") + " with args=" + (args?.GetType().Name ?? "(null)") + " and navigation args=" + (NavigationArgs?.GetType().Name ?? "(null)"));
#endif

			if ((NavigationArgs is not null) && (NavigationArgs.ReturnCounter > 0))
			{
				if (args is null)
				{
					args = new TArgs();
				}

				args.ReturnCounter = NavigationArgs.ReturnCounter + 1;
			}

			this.PushArgs(Route, args);

			try
			{
				if ((args is not null) && !string.IsNullOrEmpty(args.UniqueId))
				{
					Route += "?UniqueId=" + args.UniqueId;
				}

				return Shell.Current.GoToAsync(Route, true);
			}
			catch (Exception e)
			{
				e = Log.UnnestException(e);
				this.LogService.LogException(e);
				string ExtraInfo = Environment.NewLine + e.Message;
				return this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["FailedToNavigateToPage"], Route, ExtraInfo));
			}
		}

		/// <summary>
		/// Current page
		/// </summary>
		public Page CurrentPage => Shell.Current?.CurrentPage;
	}
}
