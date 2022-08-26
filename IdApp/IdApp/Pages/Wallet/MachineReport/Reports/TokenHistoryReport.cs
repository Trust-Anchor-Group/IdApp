﻿using NeuroFeatures;
using System;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Wallet.MachineReport.Reports
{
	/// <summary>
	/// Represent a report of the historical states of a token and the underlying state-machine.
	/// </summary>
	public class TokenHistoryReport : TokenReport
	{
		/// <summary>
		/// Represent a report of the historical states of a token and the underlying state-machine.
		/// </summary>
		/// <param name="Client">Neuro-Features client.</param>
		/// <param name="TokenId">ID of token being viewed.</param>
		public TokenHistoryReport(NeuroFeaturesClient Client, string TokenId)
			: base(Client, TokenId)
		{
		}

		/// <summary>
		/// Gets the title of report.
		/// </summary>
		/// <returns>Title</returns>
		public override Task<string> GetTitle() => Task.FromResult<string>(LocalizationResourceManager.Current["History"]);

		/// <summary>
		/// Gets the XAML for the report.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public override async Task<string> GetReportXaml()
		{
			ReportEventArgs e = await this.client.GenerateHistoryReportAsync(this.TokenId, ReportFormat.XamarinXaml);
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(LocalizationResourceManager.Current["UnableToGetHistory"]);

			return e.ReportText;
		}
	}
}
