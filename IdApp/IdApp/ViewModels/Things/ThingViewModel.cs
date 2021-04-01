using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IdApp.Navigation.Things;
using IdApp.Views.Things;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.Forms;

namespace IdApp.ViewModels.Things
{
	/// <summary>
	/// The view model to bind to when displaying a thing.
	/// </summary>
	public class ThingViewModel : BaseViewModel
	{
		private readonly INeuronService neuronService;
		private readonly INetworkService networkService;
		private readonly INavigationService navigationService;
		private readonly IUiDispatcher uiDispatcher;

		/// <summary>
		/// Creates an instance of the <see cref="ThingViewModel"/> class.
		/// </summary>
		public ThingViewModel()
			: this(null, null, null, null)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ThingViewModel"/> class.
		/// For unit tests.
		/// </summary>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="navigationService">The navigation service.</param>
		/// <param name="uiDispatcher"> The dispatcher to use for alerts and accessing the main thread.</param>
		protected internal ThingViewModel(INeuronService neuronService, INetworkService networkService, INavigationService navigationService, IUiDispatcher uiDispatcher)
		{
			this.neuronService = neuronService ?? DependencyService.Resolve<INeuronService>();
			this.networkService = networkService ?? DependencyService.Resolve<INetworkService>();
			this.navigationService = navigationService ?? DependencyService.Resolve<INavigationService>();
			this.uiDispatcher = uiDispatcher ?? DependencyService.Resolve<IUiDispatcher>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out ViewThingNavigationArgs args))
			{
			}

			AssignProperties();
			EvaluateAllCommands();
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			await base.DoUnbind();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands();
		}

	}
}