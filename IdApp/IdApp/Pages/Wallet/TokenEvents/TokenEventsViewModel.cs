using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NeuroFeatures.Events;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.TokenEvents
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of a token.
	/// </summary>
	public class TokenEventsViewModel : QrXmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="TokenEventsViewModel"/> class.
		/// </summary>
		public TokenEventsViewModel()
			: base()
		{
			this.Events = new();

			this.AddNoteCommand = new Command(async _ => await this.AddNote());
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out TokenEventsNavigationArgs args))
			{
				this.TokenId = args.TokenId;

				foreach (TokenEvent Event in args.Events)
					this.Events.Add(EventItem.Create(Event));
			}

			AssignProperties();
			EvaluateAllCommands();
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			await base.DoUnbind();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
		}

		#region Properties

		/// <summary>
		/// Events
		/// </summary>
		public ObservableCollection<EventItem> Events { get; }

		/// <summary>
		/// See <see cref="TokenId"/>
		/// </summary>
		public static readonly BindableProperty TokenIdProperty =
			BindableProperty.Create(nameof(TokenId), typeof(string), typeof(TokenEventsViewModel), default(string));

		/// <summary>
		/// Token ID
		/// </summary>
		public string TokenId
		{
			get => (string)this.GetValue(TokenIdProperty);
			set => this.SetValue(TokenIdProperty, value);
		}

		#endregion

		#region Commands

		/// <summary>
		/// Command to copy a value to the clipboard.
		/// </summary>
		public ICommand AddNoteCommand { get; }

		private async Task AddNote()
		{
			try
			{
				// TODO
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