using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;
using IdApp.Pages;
using Waher.Script.Functions.ComplexNumbers;

namespace IdApp.Services.Navigation
{
	[Singleton]
	internal sealed partial class NavigationService : LoadableService, INavigationService
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

		private async void Shell_Navigating(object Sender, ShellNavigatingEventArgs e)
		{
			try
			{
				NavigationArgs NavigationArgs = this.TryGetArgs<NavigationArgs>();

				if ((e.Source == ShellNavigationSource.Pop) &&
					(NavigationArgs is not null) &&
					(!string.IsNullOrWhiteSpace(NavigationArgs.ReturnRoute) ||
					NavigationArgs.ReturnCounter > 1))
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
						PageName += args.UniqueId;

					this.navigationArgsMap[PageName] = args;
				}
				else
					this.navigationArgsMap.Remove(PageName);
			}
		}

		public TArgs TryGetArgs<TArgs>(string UniqueId = null) where TArgs : NavigationArgs
		{
			if (!this.CanUseNavigationService)
				return null;

			TArgs Args = null;

			if (this.CurrentPage is Page Page)
			{
				Args = this.TryGetArgs<TArgs>(Page.GetType().Name, UniqueId);
				Args ??= this.TryGetArgs<TArgs>(Routing.GetRoute(Page), UniqueId);
			}

			return Args;
		}

		private TArgs TryGetArgs<TArgs>(string PageName, string UniqueId = null) where TArgs : NavigationArgs
		{
			if (!string.IsNullOrEmpty(UniqueId))
				PageName += UniqueId;

			if (this.TryGetPageName(PageName, out string pageName) &&
				this.navigationArgsMap.TryGetValue(pageName, out NavigationArgs NavigationArgs) &&
				(NavigationArgs is not null))
			{
				TArgs Args = NavigationArgs as TArgs;
				return Args;
			}

			return null;
		}

		public Task GoBackAsync(bool Animate = true)
		{
			if (!this.CanUseNavigationService)
				return Task.CompletedTask;

			try
			{
				NavigationArgs NavigationArgs = this.TryGetArgs<NavigationArgs>();
				string ReturnRoute = defaultGoBackRoute;
				int ReturnCounter = 0;

				if (NavigationArgs is not null)
				{
					ReturnCounter = NavigationArgs.ReturnCounter;

					if ((ReturnCounter == 0) && !string.IsNullOrEmpty(NavigationArgs.ReturnRoute))
						ReturnRoute = NavigationArgs.ReturnRoute;
				}

				if (ReturnCounter > 1)
				{
					for (int i = 1; i < ReturnCounter; i++)
						ReturnRoute += "/" + defaultGoBackRoute;
				}

				return Shell.Current.GoToAsync(ReturnRoute, Animate);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
				return this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["FailedToClosePage"]);
			}
		}

		public Task GoToAsync<TArgs>(string Route, TArgs Args = null, BackMethod BackMethod = BackMethod.Inherited, string UniqueId = null) where TArgs : NavigationArgs, new()
		{
			if (!this.CanUseNavigationService)
				return Task.CompletedTask;

			// Get the parent's navigation arguments
			NavigationArgs ParentArgs = this.TryGetArgs<NavigationArgs>();

			if ((Args is not null) && Args.CancelReturnCounter)
			{
				// ignore the previous args if the return counter was canceled
				ParentArgs = null;
			}

			if ((ParentArgs is not null) && (ParentArgs.ReturnCounter > 0))
			{
				Args ??= new TArgs();
				Args.ReturnCounter = ParentArgs.ReturnCounter + 1;
			}

			this.PushArgs(Route, Args);

			try
			{
				if ((Args is not null) && !string.IsNullOrEmpty(Args.UniqueId))
					Route += "?UniqueId=" + Args.UniqueId;

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
