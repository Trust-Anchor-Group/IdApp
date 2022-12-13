﻿using IdApp.Services.Xmpp;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Wallet.MachineReport.Reports
{
	/// <summary>
	/// Represent a profiling report of a token and the underlying state-machine.
	/// </summary>
	public class TokenProfilingReport : TokenReport
	{
		/// <summary>
		/// Represent a profiling report of a token and the underlying state-machine.
		/// </summary>
		/// <param name="XmppService">XMPP Service reference.</param>
		/// <param name="TokenId">ID of token being viewed.</param>
		public TokenProfilingReport(IXmppService XmppService, string TokenId)
			: base(XmppService, TokenId)
		{
		}

		/// <summary>
		/// Gets the title of report.
		/// </summary>
		/// <returns>Title</returns>
		public override Task<string> GetTitle() => Task.FromResult<string>(LocalizationResourceManager.Current["Profiling"]);

		/// <summary>
		/// Gets the XAML for the report.
		/// </summary>
		/// <returns>String-representation of XAML of report.</returns>
		public override Task<string> GetReportXaml()
		{
			return this.xmppService.GenerateNeuroFeatureProfilingReport(this.TokenId);
		}
	}
}
