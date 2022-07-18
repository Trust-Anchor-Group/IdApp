using System.Threading.Tasks;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MachineReport
{
	/// <summary>
	/// The view model to bind to for when displaying information about the current state of a state-machine.
	/// </summary>
	public class MachineReportViewModel : BaseViewModel
	{
		/// <summary>
		/// The view model to bind to for when displaying information about the current state of a state-machine.
		/// </summary>
		public MachineReportViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out MachineReportNavigationArgs args))
			{
				this.Title = args.Title;
				this.Report = args.Report;
			}
		}

		#region Properties

		/// <summary>
		/// See <see cref="Title"/>
		/// </summary>
		public static readonly BindableProperty TitleProperty =
			BindableProperty.Create(nameof(Title), typeof(string), typeof(MachineReportViewModel), default);

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		public string Title
		{
			get => (string)this.GetValue(TitleProperty);
			set => this.SetValue(TitleProperty, value);
		}

		/// <summary>
		/// See <see cref="Report"/>
		/// </summary>
		public static readonly BindableProperty ReportProperty =
			BindableProperty.Create(nameof(Report), typeof(object), typeof(MachineReportViewModel), default);

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		public object Report
		{
			get => this.GetValue(ReportProperty);
			set => this.SetValue(ReportProperty, value);
		}

		#endregion

	}
}
