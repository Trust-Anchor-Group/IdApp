using IdApp.Resx;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// Represents a state-machine variable.
	/// </summary>
	public class VariableModel : BaseViewModel
	{
		/// <summary>
		/// Represents a state-machine variable.
		/// </summary>
		/// <param name="Name">Name of variable</param>
		/// <param name="Value">Value of variable</param>
		/// <param name="AsScript">Value as script</param>
		public VariableModel(string Name, object Value, string AsScript)
		{
			this.Name = Name;
			this.Value = Value;
			this.AsScript = AsScript;

			this.CopyToClipboardCommand = new Command(async _ => await this.CopyToClipboard());
		}

		/// <summary>
		/// Name of variable
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Value of variable
		/// </summary>
		public object Value { get; }

		/// <summary>
		/// Value as script
		/// </summary>
		public string AsScript { get; }

		#region Commands

		/// <summary>
		/// Command to copy a value to the clipboard.
		/// </summary>
		public ICommand CopyToClipboardCommand { get; }

		private async Task CopyToClipboard()
		{
			try
			{
				await Clipboard.SetTextAsync(this.AsScript);
				await this.UiSerializer.DisplayAlert(AppResources.SuccessTitle, AppResources.TagValueCopiedToClipboard);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		#endregion
	}
}
