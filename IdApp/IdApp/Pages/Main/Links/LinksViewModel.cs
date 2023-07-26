using IdApp.Pages.Main.Link;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Links
{
	/// <summary>
	/// The view model to bind to for when displaying links.
	/// </summary>
	public class LinksViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="LinksViewModel"/> class.
		/// </summary>
		public LinksViewModel()
			: base()
		{
			this.OpenLinkCommand = new Command(async P => await this.OpenLink((string)P));
		}

		#region Commands

		/// <summary>
		/// Command executed when user wants to open a specific link.
		/// </summary>
		public ICommand OpenLinkCommand { get; }

		private Task OpenLink(string Url)
		{
			int i = Url.IndexOf(';');
			string Title;

			if (i < 0)
				Title = string.Empty;
			else
			{
				Title = Url.Substring(0, i);
				Url = Url[(i + 1)..];
			}

			return this.NavigationService.GoToAsync(nameof(LinkPage), new LinkNavigationArgs(Url, Title));
		}

		#endregion
	}
}
