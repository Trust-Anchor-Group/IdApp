using EDaler;
using IdApp.Services.Navigation;
using System.Threading.Tasks;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Holds navigation parameters specific to the eDaler wallet.
	/// </summary>
	public class ServiceProvidersNavigationArgs : NavigationArgs
    {
		private readonly IBuyEDalerServiceProvider[] serviceProviders;
		private readonly TaskCompletionSource<IBuyEDalerServiceProvider> selected;
		private readonly string description;

		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersNavigationArgs"/> class.
		/// </summary>
		public ServiceProvidersNavigationArgs()
			: this(new IBuyEDalerServiceProvider[0], string.Empty)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersNavigationArgs"/> class.
		/// </summary>
		/// <param name="ServiceProviders">Service Providers</param>
		/// <param name="Description">Descriptive text to show the user.</param>
		public ServiceProvidersNavigationArgs(IBuyEDalerServiceProvider[] ServiceProviders, string Description)
        {
			this.serviceProviders = ServiceProviders;
			this.description = Description;
            this.selected = new();
        }

		/// <summary>
		/// Available service providers.
		/// </summary>
		public IBuyEDalerServiceProvider[] ServiceProviders => this.serviceProviders;

		/// <summary>
		/// Descriptive text to show the user.
		/// </summary>
		public string Description => this.description;

		/// <summary>
		/// Task completion source, waiting for a response from the user.
		/// </summary>
		public TaskCompletionSource<IBuyEDalerServiceProvider> Selected => this.selected;

        /// <summary>
        /// Waits for the service provider to be selected; null is returned if the user goes back.
        /// </summary>
        /// <returns>Selected service provider, or null if user cancels, by going back.</returns>
        public Task<IBuyEDalerServiceProvider> WaitForServiceProviderSelection()
        {
            return this.selected?.Task ?? Task.FromResult<IBuyEDalerServiceProvider>(null);
        }
    }
}
