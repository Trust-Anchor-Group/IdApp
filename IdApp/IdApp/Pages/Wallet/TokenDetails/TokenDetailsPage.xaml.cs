using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A page that allows the user to view information about a token.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenDetailsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TokenDetailsPage"/> class.
		/// </summary>
		public TokenDetailsPage()
		{
			this.ViewModel = new TokenDetailsViewModel(this);

			InitializeComponent();
		}

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled</returns>
		protected override bool OnBackButtonPressed()
		{
			this.ViewModel.NavigationService.GoBackAsync();
			return true;
		}

		/// <summary>
		/// Adds a Legal ID record in the parts section.
		/// 
		/// (Grouped collections do not work. Need to do this manually. TODO: MVVC when this is possible.)
		/// </summary>
		/// <param name="Model">View model</param>
		/// <param name="Label">Label</param>
		/// <param name="FriendlyName">Friendly name</param>
		/// <param name="LegalId">Legal ID</param>
		internal void AddLegalId(TokenDetailsViewModel Model, string Label, string FriendlyName, string LegalId)
		{
			int Row = this.PartsGrid.RowDefinitions.Count;
			Label Lbl;

			this.PartsGrid.RowDefinitions.Add(new RowDefinition()
			{
				Height = GridLength.Auto
			});

			this.PartsGrid.Children.Add(new Label()
			{
				Text = Label,
				Style = (Style)App.Current.Resources["KeyLabel"]
			}, 0, Row);

			this.PartsGrid.Children.Add(Lbl = new Label()
			{
				Text = FriendlyName,
				Style = (Style)App.Current.Resources["ClickableValueLabel"]
			}, 1, Row);

			TapGestureRecognizer Tap = new();
			Lbl.GestureRecognizers.Add(Tap);
			Tap.Command = Model.ViewIdCommand;
			Tap.CommandParameter = LegalId;
		}

		/// <summary>
		/// Adds a JID record in the parts section.
		/// 
		/// (Grouped collections do not work. Need to do this manually. TODO: MVVC when this is possible.)
		/// </summary>
		/// <param name="Model">View model</param>
		/// <param name="Label">Label</param>
		/// <param name="Jid">XMPP Network address (JID)</param>
		/// <param name="LegalId">Legal ID</param>
		/// <param name="FriendlyName">Friendly name</param>
		internal void AddJid(TokenDetailsViewModel Model, string Label, string Jid, string LegalId, string FriendlyName)
		{
			int Row = this.PartsGrid.RowDefinitions.Count;
			Label Lbl;

			this.PartsGrid.RowDefinitions.Add(new RowDefinition()
			{
				Height = GridLength.Auto
			});

			this.PartsGrid.Children.Add(new Label()
			{
				Text = Label,
				Style = (Style)App.Current.Resources["KeyLabel"]
			}, 0, Row);

			this.PartsGrid.Children.Add(Lbl = new Label()
			{
				Text = Jid,
				Style = (Style)App.Current.Resources["ClickableValueLabel"]
			}, 1, Row);

			TapGestureRecognizer Tap = new();
			Lbl.GestureRecognizers.Add(Tap);
			Tap.Command = Model.OpenChatCommand;
			Tap.CommandParameter = Jid + " | " + LegalId + " | " + FriendlyName;
		}
	}
}