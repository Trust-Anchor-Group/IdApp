using IdApp.Pages.Wallet.MachineReport.Reports;
using System;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MachineReport
{
	/// <summary>
	/// The view model to bind to for when displaying information about the current state of a state-machine.
	/// </summary>
	public class MachineReportViewModel : BaseViewModel, IDisposable
	{
		/// <summary>
		/// The view model to bind to for when displaying information about the current state of a state-machine.
		/// </summary>
		public MachineReportViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out MachineReportNavigationArgs args))
			{
				this.TokenReport = args.Report;
				this.Title = await this.TokenReport.GetTitle();
				await this.TokenReport.GenerateReport(this);
			}

			this.XmppService.Wallet.VariablesUpdated += this.Wallet_VariablesUpdated;
			this.XmppService.Wallet.StateUpdated += this.Wallet_StateUpdated;
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			this.XmppService.Wallet.VariablesUpdated -= this.Wallet_VariablesUpdated;
			this.XmppService.Wallet.StateUpdated -= this.Wallet_StateUpdated;

			this.DeleteTemporaryFiles();

			return base.OnDispose();
		}

		private Task Wallet_StateUpdated(object Sender, NeuroFeatures.NewStateEventArgs e)
		{
			return this.UpdateReport();
		}

		private Task Wallet_VariablesUpdated(object Sender, NeuroFeatures.VariablesUpdatedEventArgs e)
		{
			return this.UpdateReport();
		}

		private Task UpdateReport()
		{
			return this.TokenReport.GenerateReport(this);
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

		/// <summary>
		/// See <see cref="TokenReport"/>
		/// </summary>
		public static readonly BindableProperty TokenReportProperty =
			BindableProperty.Create(nameof(TokenReport), typeof(TokenReport), typeof(MachineReportViewModel), default);

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		public TokenReport TokenReport
		{
			get => (TokenReport)this.GetValue(TokenReportProperty);
			set => this.SetValue(TokenReportProperty, value);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		void IDisposable.Dispose()
		{
			this.DeleteTemporaryFiles();
		}

		private void DeleteTemporaryFiles()
		{
			this.TokenReport?.DeleteTemporaryFiles();
		}

		#endregion

	}
}
