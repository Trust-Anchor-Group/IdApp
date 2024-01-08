using FFImageLoading.Forms;
using FFImageLoading.Svg.Forms;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Service Provider information model, including related notification information.
	/// </summary>
	public class ServiceProviderModel
	{
		private readonly IServiceProvider serviceProvider;

		/// <summary>
		/// Service Provider information model, including related notification information.
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
		/// If an image should be displayed.
		/// </summary>
		public bool ShowImage => this.HasIcon;

		/// <summary>
		/// If text should be displayed.
		/// </summary>
		public bool ShowText => !this.HasIcon || this.IconWidth <= 250 || this.serviceProvider.GetType().Assembly == typeof(App).Assembly;

		/// <summary>
		/// Icon URL Source
		/// </summary>
		public ImageSource IconUrlSource
		{
			get
			{
				ImageSource Result;

				if (this.IconUrl.StartsWith("resource://"))
				{
					string Resource = this.IconUrl[11..];

					if (Resource.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase))
						Result = SvgImageSource.FromResource(Resource);
					else
						Result = ImageSource.FromResource(Resource);
				}
				else
				{
					if (this.IconUrl.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase))
						Result = SvgImageSource.FromUri(new System.Uri(this.IconUrl));
					else
						Result = new DataUrlImageSource(this.IconUrl);
				}

				return Result;
			}
		}

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
