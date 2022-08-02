using IdApp.Resx;
using NeuroFeatures;
using System;
using System.Threading.Tasks;

namespace IdApp.Pages.Wallet.MachineReport.Reports
{
	/// <summary>
	/// Represent a state diagram of a token and the underlying state-machine.
	/// </summary>
	public class TokenStateDiagramReport : TokenReport
	{
		/// <summary>
		/// Represent a state diagram of a token and the underlying state-machine.
		/// </summary>
		/// <param name="Client">Neuro-Features client.</param>
		/// <param name="TokenId">ID of token being viewed.</param>
		public TokenStateDiagramReport(NeuroFeaturesClient Client, string TokenId)
			: base(Client, TokenId)
		{
		}

		/// <summary>
		/// Gets the title of report.
		/// </summary>
		/// <returns>Title</returns>
		public override Task<string> GetTitle() => Task.FromResult<string>(AppResources.StateDiagram);

		/// <summary>
		/// Gets the XAML for the report.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public override async Task<string> GetReportXaml()
		{
			ReportEventArgs e = await this.client.GenerateStateDiagramAsync(this.TokenId, ReportFormat.XamarinXaml);
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(AppResources.UnableToGetStateDiagram);

			return e.ReportText;
		}
	}
}
