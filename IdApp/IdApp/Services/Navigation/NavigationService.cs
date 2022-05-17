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
		private const string DefaultGoBackRoute = "..";
		private NavigationArgs currentNavigationArgs;
		private readonly Dictionary<string, NavigationArgs> navigationArgsMap;
		bool isManuallyNavigatingBack = false;

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
					Shell.Current.Navigating += Shell_Navigating;

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
					Shell.Current.Navigating -= Shell_Navigating;
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

		private bool TryGetPageName(string route, out string pageName)
		{
			pageName = null;
			if (!string.IsNullOrWhiteSpace(route))
			{
				pageName = route.TrimStart('.', '/');
				return !string.IsNullOrWhiteSpace(pageName);
			}

			return false;
		}

		internal void PushArgs<TArgs>(string route, TArgs args) where TArgs : NavigationArgs
		{
			this.currentNavigationArgs = args;

			if (this.TryGetPageName(route, out string pageName))
			{
				if (!(args is null))
					this.navigationArgsMap[pageName] = args;
				else
					this.navigationArgsMap.Remove(pageName);
			}
		}

		public bool TryPopArgs<TArgs>(out TArgs args) where TArgs : NavigationArgs
		{
			string route = Shell.Current.CurrentPage?.GetType().Name;
			return this.TryPopArgs(route, out args);
		}

		public TArgs GetPopArgs<TArgs>() where TArgs : NavigationArgs
		{
			string route = Shell.Current.CurrentPage?.GetType().Name;

			if (TryPopArgs<TArgs>(route, out TArgs args))
			{
				return args;
			}

			return null;
		}

		internal bool TryPopArgs<TArgs>(string route, out TArgs args) where TArgs : NavigationArgs
		{
			args = default;
			if (this.TryGetPageName(route, out string pageName) &&
				this.navigationArgsMap.TryGetValue(pageName, out NavigationArgs navArgs) &&
				!(navArgs is null))
			{
				args = navArgs as TArgs;
			}

			return !(args is null);
		}

		public Task GoBackAsync()
		{
			return this.GoBackAsync(true);
		}

		public async Task GoBackAsync(bool Animate)
		{
			try
			{
				string ReturnRoute = DefaultGoBackRoute;
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
						ReturnRoute += "/" + DefaultGoBackRoute;
					}
				}

				await Shell.Current.GoToAsync(ReturnRoute, Animate);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, AppResources.FailedToClosePage);
			}
		}

		public Task GoToAsync(string route)
		{
			return GoToAsync(route, (NavigationArgs)null, (NavigationArgs)null);
		}

		public Task GoToAsync<TArgs>(string route, TArgs args) where TArgs : NavigationArgs, new()
		{
			NavigationArgs navigationArgs = this.GetPopArgs<NavigationArgs>();

			if ((args is not null) && args.CancelReturnCounter)
            {
				// ignore the previous args if the return counter was canceled
				navigationArgs = null;
            }

			return GoToAsync(route, args, navigationArgs);
		}

		internal async Task GoToAsync<TArgs>(string route, TArgs args, NavigationArgs navigationArgs) where TArgs : NavigationArgs, new()
		{
			if ((navigationArgs is not null) && (navigationArgs.ReturnCounter > 0))
			{
				if (args is null)
				{
					args = new TArgs();
				}

				args.ReturnCounter = navigationArgs.ReturnCounter + 1;
			}

			this.PushArgs(route, args);

			try
			{
				await Shell.Current.GoToAsync(route, true);
			}
			catch (Exception e)
			{
				e = Log.UnnestException(e);
				this.LogService.LogException(e);
				string extraInfo = Environment.NewLine + e.Message;
				await this.UiSerializer.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.FailedToNavigateToPage, route, extraInfo));
			}
		}

		/// <summary>
		/// Current page
		/// </summary>
		public Page CurrentPage => Shell.Current.CurrentPage;
	}
}