using IdApp.Services.Navigation;

namespace IdApp.Pages.Wallet.MachineReport
{
	/// <summary>
	/// Holds navigation parameters specific to a report from a state-machine.
	/// </summary>
	public class MachineReportNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MachineReportNavigationArgs"/> class.
        /// </summary>
        public MachineReportNavigationArgs() { }

		/// <summary>
		/// Creates a new instance of the <see cref="MachineReportNavigationArgs"/> class.
		/// </summary>
		/// <param name="Title">Title of report</param>
		/// <param name="Report">Report content</param>
		public MachineReportNavigationArgs(string Title, object Report)
        {
			this.Title = Title;
			this.Report = Report;
        }

		/// <summary>
		/// Title of report
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Report content
		/// </summary>
		public object Report { get; }
	}
}
