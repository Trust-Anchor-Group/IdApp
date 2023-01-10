using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Wallet.MyWallet.ObjectModels;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MyWallet
{
	/// <summary>
	/// Data Template Selector, based on Token Item Type.
	/// </summary>
	public class TokenItemTypeTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template to use for notifications
		/// </summary>
		public DataTemplate NotificationTemplate { get; set; }

		/// <summary>
		/// Template to use for tokens.
		/// </summary>
		public DataTemplate TokenTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			if (item is TokenItem)
				return this.TokenTemplate ?? this.DefaultTemplate;
			else if (item is EventModel)
				return this.NotificationTemplate ?? this.DefaultTemplate;
			else
				return this.DefaultTemplate;
		}
	}
}
