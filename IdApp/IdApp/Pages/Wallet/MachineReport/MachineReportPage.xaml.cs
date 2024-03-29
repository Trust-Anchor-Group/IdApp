﻿using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Wallet.MachineReport
{
	/// <summary>
	/// A page that allows the user to view a state-machine report.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MachineReportPage
	{
		/// <summary>
		/// A page that allows the user to view a state-machine report.
		/// </summary>
		public MachineReportPage()
		{
			this.ViewModel = new MachineReportViewModel();

			this.InitializeComponent();
		}
	}
}
