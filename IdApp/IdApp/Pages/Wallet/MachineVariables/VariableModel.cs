using IdApp.Resx;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Script;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// Represents a state-machine variable.
	/// </summary>
	public class VariableModel : BaseViewModel
	{
		private readonly string name;
		private object value;
		private string asScript;

		/// <summary>
		/// Represents a state-machine variable.
		/// </summary>
		/// <param name="Name">Name of variable</param>
		/// <param name="Value">Value of variable</param>
		public VariableModel(string Name, object Value)
		{
			this.name = Name;
			this.value = Value;
			this.asScript = Expression.ToString(Value);

			this.CopyToClipboardCommand = new Command(async _ => await this.CopyToClipboard());
		}

		/// <summary>
		/// Name of variable
		/// </summary>
		public string Name => this.name;

		/// <summary>
		/// Value of variable
		/// </summary>
		public object Value
		{
			get => this.value;
			private set
			{
				this.OnPropertyChanging(nameof(this.Value));
				this.value = value;
				this.OnPropertyChanged(nameof(this.Value));
			}
		}

		/// <summary>
		/// Value as script
		/// </summary>
		public string AsScript
		{
			get => this.asScript;
			private set
			{
				this.OnPropertyChanging(nameof(this.AsScript));
				this.asScript = value;
				this.OnPropertyChanged(nameof(this.AsScript));
			}
		}

		/// <summary>
		/// Updates the value of the variable.
		/// </summary>
		/// <param name="Value">Value of variable</param>
		public void UpdateValue(object Value)
		{
			this.Value = Value;
			this.AsScript = Expression.ToString(Value);
		}

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
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["TagValueCopiedToClipboard"]);
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
