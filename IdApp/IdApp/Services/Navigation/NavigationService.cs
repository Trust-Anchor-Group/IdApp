using IdApp.Resx;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Inventory;
using Xamarin.Forms;

namespace IdApp.Services.Navigation
{
	[Singleton]
	internal sealed class NavigationService : LoadableService, INavigationService
	{
		private const string defaultGoBackRoute = "..";
		private readonly Dictionary<string, NavigationArgs> navigationArgsMap;
		private NavigationArgs currentNavigationArgs;
		private bool isManuallyNavigatingBack = false;

		public NavigationService()
		{
			this.navigationArgsMap = new Dictionary<string, NavigationArgs>();
		}

		///<inheritdoc/>
		public override Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			if (this.BeginLoad(cancellationToken))
			{
				try
				{
					Shell.Current.Navigating += this.Shell_Navigating;

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
					Shell.Current.Navigating -= this.Shell_Navigating;
				}
				catch (Exception e)
				{
					this.LogService.LogException(e);
				}

				this.EndUnload();
			}

			return Task.CompletedTask;
		}


		private async void Shell_Navigating(object sender, ShellNavigatingEventArgs e)
		{
			if ((e.Source == ShellNavigationSource.Pop) &&
				(this.currentNavigationArgs is not null) &&
				(!string.IsNullOrWhiteSpace(this.currentNavigationArgs.ReturnRoute) ||
				this.currentNavigationArgs.ReturnCounter > 1))
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
			this.currentNavigationArgs = args;

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

		public bool TryPopArgs<TArgs>(out TArgs args, string UniqueId = null) where TArgs : NavigationArgs
		{
			string PageName = Shell.Current.CurrentPage?.GetType().Name;

			return this.TryPopArgs(PageName, out args, UniqueId);
		}

		public TArgs GetPopArgs<TArgs>(string UniqueId = null) where TArgs : NavigationArgs
		{
			string PageName = Shell.Current.CurrentPage?.GetType().Name;

			if (this.TryPopArgs<TArgs>(PageName, out TArgs args, UniqueId))
			{
				return args;
			}

			return null;
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

			try
			{
				string ReturnRoute = defaultGoBackRoute;
				int ReturnCounter = 0;

				if (this.currentNavigationArgs is not null)
				{
					ReturnCounter = this.currentNavigationArgs.ReturnCounter;

					if ((ReturnCounter == 0) &&
						!string.IsNullOrEmpty(this.currentNavigationArgs.ReturnRoute))
					{
						ReturnRoute = this.currentNavigationArgs.ReturnRoute;
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
				return this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToClosePage);
			}
		}

		public Task GoToAsync(string Route)
		{
			return this.GoToAsync(Route, (NavigationArgs)null, (NavigationArgs)null);
		}

		public Task GoToAsync<TArgs>(string Route, TArgs args) where TArgs : NavigationArgs, new()
		{
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
				return this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToPage, Route, ExtraInfo));
			}
		}

		/// <summary>
		/// Current page
		/// </summary>
		public Page CurrentPage => Shell.Current.CurrentPage;
	}
}
