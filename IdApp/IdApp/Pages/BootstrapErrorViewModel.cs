using System;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages
{
	internal class BootstrapErrorViewModel : BaseViewModel
	{
		public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(BootstrapErrorViewModel));

		public static readonly BindableProperty StackTraceProperty = BindableProperty.Create(nameof(StackTrace), typeof(string), typeof(BootstrapErrorViewModel));

		public BootstrapErrorViewModel()
		{
			this.CopyToClipboardCommand = CommandFactory.Create(this.CopyToClipboard);
		}

		public string Title
		{
			get => (string)this.GetValue(TitleProperty);
			set => this.SetValue(TitleProperty, value);
		}

		public string StackTrace
		{
			get => (string)this.GetValue(StackTraceProperty);
			set => this.SetValue(StackTraceProperty, value);
		}

		public ICommand CopyToClipboardCommand { get; }

		private async void CopyToClipboard()
		{
			try
			{
				await Clipboard.SetTextAsync(this.StackTrace);
			}
			catch (Exception Exception)
			{
				this.LogService?.LogException(Exception);
			}
		}
	}
}
