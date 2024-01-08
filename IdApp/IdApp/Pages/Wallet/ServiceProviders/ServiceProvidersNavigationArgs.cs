using IdApp.Services.Navigation;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Holds navigation parameters for selecting a service provider.
	/// </summary>
	public class ServiceProvidersNavigationArgs : NavigationArgs
	{
		private readonly IServiceProvider[] serviceProviders;
		private readonly string title;
		private readonly string description;

		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersNavigationArgs"/> class.
		/// </summary>
		public ServiceProvidersNavigationArgs()
			: this(new IServiceProvider[0], string.Empty, string.Empty)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersNavigationArgs"/> class.
		/// </summary>
		/// <param name="ServiceProviders">Service Providers</param>
		/// <param name="Title">Title to show the user.</param>
		/// <param name="Description">Descriptive text to show the user.</param>
		public ServiceProvidersNavigationArgs(IServiceProvider[] ServiceProviders, string Title, string Description)
		{
			this.serviceProviders = ServiceProviders;
			this.title = Title;
			this.description = Description;
			this.ServiceProvider = new();
		}

		/// <summary>
		/// Available service providers.
		/// </summary>
		public IServiceProvider[] ServiceProviders => this.serviceProviders;

		/// <summary>
		/// Title to show the user.
		/// </summary>
		public string Title => this.title;

		/// <summary>
		/// Descriptive text to show the user.
		/// </summary>
		public string Description => this.description;

		/// <summary>
		/// Task completion source; can be used to wait for a result.
		/// </summary>
		public TaskCompletionSource<IServiceProvider> ServiceProvider { get; internal set; }
	}
}
