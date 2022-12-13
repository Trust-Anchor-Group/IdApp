using IdApp.Services.Xmpp;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Wallet.MachineReport.Reports
{
	/// <summary>
	/// Represent a report of the present state of a token and the underlying state-machine.
	/// </summary>
	public class TokenPresentReport : TokenReport
	{
		/// <summary>
		/// Represent a report of the present state of a token and the underlying state-machine.
		/// </summary>
		/// <param name="XmppService">XMPP Service reference.</param>
		/// <param name="TokenId">ID of token being viewed.</param>
		public TokenPresentReport(IXmppService XmppService, string TokenId)
			: base(XmppService, TokenId)
		{
		}

		/// <summary>
		/// Gets the title of report.
		/// </summary>
		/// <returns>Title</returns>
		public override Task<string> GetTitle() => Task.FromResult<string>(LocalizationResourceManager.Current["Present"]);

		/// <summary>
		/// Gets the XAML for the report.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public override Task<string> GetReportXaml()
		{
			return this.xmppService.GenerateNeuroFeaturePresentReport(this.TokenId);
		}
	}
}
