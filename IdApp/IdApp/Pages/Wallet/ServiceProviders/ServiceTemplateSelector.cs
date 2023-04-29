using Xamarin.Forms;

namespace IdApp.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Data Template Selector, based on Service information.
	/// </summary>
	public class ServiceTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template for services with an internal resource icon.
		/// </summary>
		public DataTemplate InternalTemplate { get; set; }

		/// <summary>
		/// Template for services with external icons.
		/// </summary>
		public DataTemplate ExternalTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			return this.DefaultTemplate;
		}
	}
}
