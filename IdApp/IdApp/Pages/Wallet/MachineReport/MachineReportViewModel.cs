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
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out MachineReportNavigationArgs args))
			{
				this.Title = args.Title;
				this.Report = args.Report;
				this.TemporaryFiles = args.TemporaryFiles;
			}
		}

		/// <inheritdoc/>
		protected override Task DoUnbind()
		{
			this.DeleteTemporaryFiles();

			return base.DoUnbind();
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
		/// See <see cref="TemporaryFiles"/>
		/// </summary>
		public static readonly BindableProperty TemporaryFilesProperty =
			BindableProperty.Create(nameof(TemporaryFiles), typeof(string[]), typeof(MachineReportViewModel), default);

		/// <summary>
		/// Temporary Files. Will be deleted when view is closed.
		/// </summary>
		public string[] TemporaryFiles
		{
			get => (string[])this.GetValue(TemporaryFilesProperty);
			set => this.SetValue(TemporaryFilesProperty, value);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.DeleteTemporaryFiles();
		}

		private void DeleteTemporaryFiles()
		{
			foreach (string FileName in this.TemporaryFiles)
			{
				try
				{
					File.Delete(FileName);
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}

			this.TemporaryFiles = new string[0];
		}

		#endregion

	}
}
