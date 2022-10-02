using EDaler;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Empty service provider. Used to show the caller the user wants to request a service from a user.
	/// </summary>
	internal class EmptyServiceProvider : IBuyEDalerServiceProvider
	{
		public string TemplateContractId => string.Empty;
		public string Id => string.Empty;
		public string Type => string.Empty;
		public string Name => string.Empty;
		public string IconUrl => string.Empty;
		public int IconWidth => 0;
		public int IconHeight => 0;
	}
}
