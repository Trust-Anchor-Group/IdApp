using EDaler;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Contact Information model, including related notification information.
	/// </summary>
	public class ServiceProviderModel
	{
		private readonly IServiceProvider serviceProvider;

		/// <summary>
		/// Contact Information model, including related notification information.
		/// </summary>
		/// <param name="ServiceProvider">Contact information.</param>
		public ServiceProviderModel(IServiceProvider ServiceProvider)
		{
			this.serviceProvider = ServiceProvider;
		}

		/// <summary>
		/// Underlying service provider
		/// </summary>
		public IServiceProvider ServiceProvider => this.serviceProvider;

		/// <summary>
		/// Service ID
		/// </summary>
		public string Id => this.serviceProvider.Id;

		/// <summary>
		/// Displayable name
		/// </summary>
		public string Name => this.serviceProvider.Name;

		/// <summary>
		/// If service provider has an icon
		/// </summary>
		public bool HasIcon => !string.IsNullOrEmpty(this.serviceProvider.IconUrl);

		/// <summary>
		/// Icon URL
		/// </summary>
		public string IconUrl => this.serviceProvider.IconUrl;

		/// <summary>
		/// Icon Width
		/// </summary>
		public int IconWidth => this.serviceProvider.IconWidth;

		/// <summary>
		/// Icon Height
		/// </summary>
		public int IconHeight => this.serviceProvider.IconHeight;
	}
}
