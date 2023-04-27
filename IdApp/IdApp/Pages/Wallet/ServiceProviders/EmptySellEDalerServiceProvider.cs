using EDaler;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Empty service provider. Used to show the caller the user wants to request a service from a user.
	/// </summary>
	internal class EmptySellEDalerServiceProvider : ISellEDalerServiceProvider
	{
		public string SellEDalerTemplateContractId => string.Empty;
		public string Id => string.Empty;
		public string Type => string.Empty;
		public string Name => string.Empty;
		public string IconUrl => string.Empty;
		public int IconWidth => 0;
		public int IconHeight => 0;

		/// <summary>
		/// Creates a new instance of the service provider.
		/// </summary>
		/// <param name="Id">ID of service provider.</param>
		/// <param name="Type">Type of service provider.</param>
		/// <param name="Name">Displayable name of service provider.</param>
		public IServiceProvider Create(string Id, string Type, string Name)
		{
			return new EmptySellEDalerServiceProvider();
		}

		/// <summary>
		/// Creates a new instance of the service provider.
		/// </summary>
		/// <param name="Id">ID of service provider.</param>
		/// <param name="Type">Type of service provider.</param>
		/// <param name="Name">Displayable name of service provider.</param>
		/// <param name="IconUrl">Optional URL to icon of service provider.</param>
		/// <param name="IconWidth">Width of icon, if available.</param>
		/// <param name="IconHeight">Height of icon, if available.</param>
		public IServiceProvider Create(string Id, string Type, string Name, string IconUrl, int IconWidth, int IconHeight)
		{
			return new EmptySellEDalerServiceProvider();
		}

		/// <summary>
		/// Creates a new instance of the service provider.
		/// </summary>
		/// <param name="Id">ID of service provider.</param>
		/// <param name="Type">Type of service provider.</param>
		/// <param name="Name">Displayable name of service provider.</param>
		/// <param name="TemplateContractId">Contract ID of template.</param>
		public IServiceProviderWithTemplate Create(string Id, string Type, string Name, string TemplateContractId)
		{
			return new EmptySellEDalerServiceProvider();
		}

		/// <summary>
		/// Creates a new instance of the service provider.
		/// </summary>
		/// <param name="Id">ID of service provider.</param>
		/// <param name="Type">Type of service provider.</param>
		/// <param name="Name">Displayable name of service provider.</param>
		/// <param name="IconUrl">Optional URL to icon of service provider.</param>
		/// <param name="IconWidth">Width of icon, if available.</param>
		/// <param name="IconHeight">Height of icon, if available.</param>
		/// <param name="TemplateContractId">Contract ID of template.</param>
		public IServiceProviderWithTemplate Create(string Id, string Type, string Name, string IconUrl, int IconWidth, int IconHeight, string TemplateContractId)
		{
			return new EmptySellEDalerServiceProvider();
		}
	}
}
