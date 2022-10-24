using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Link
{
	/// <summary>
	/// The view model to bind to for when displaying links.
	/// </summary>
	public class LinkViewModel : QrXmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="LinkViewModel"/> class.
		/// </summary>
		public LinkViewModel()
			: base()
		{
		}

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult(this.TitleString);

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out LinkNavigationArgs args))
			{
				if (string.IsNullOrEmpty(args.Url))
				{
					this.TitleString = LocalizationResourceManager.Current["Custom"];
				}
				else
				{
					this.TitleString = args.Title;
					this.GenerateQrCode(args.Url);
				}
			}
		}

		/// <summary>
		/// See <see cref="TitleString"/>
		/// </summary>
		public static readonly BindableProperty TitleStringProperty =
			BindableProperty.Create(nameof(TitleString), typeof(string), typeof(LinkViewModel), default(string));

		/// <summary>
		/// Current entry
		/// </summary>
		public string TitleString
		{
			get => (string)this.GetValue(TitleStringProperty);
			set => this.SetValue(TitleStringProperty, value);
		}

		/// <summary>
		/// If App links should be encoded with the link.
		/// </summary>
		public override bool EncodeAppLinks => false;

	}
}
