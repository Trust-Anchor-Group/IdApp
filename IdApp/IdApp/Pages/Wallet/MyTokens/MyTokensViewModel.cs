using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MyTokens
{
	/// <summary>
	/// The view model to bind to for when displaying my tokens.
	/// </summary>
	public class MyTokensViewModel : XmppViewModel
	{
		private TaskCompletionSource<TokenItem> selected;

		/// <summary>
		/// Creates an instance of the <see cref="MyTokensViewModel"/> class.
		/// </summary>
		public MyTokensViewModel()
			: base()
		{
			this.BackCommand = new Command(async _ => await GoBack());
			this.LoadMoreTokensCommand = new Command(async _ => await LoadMoreTokens());

			this.Tokens = new ObservableCollection<TokenItem>();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out MyTokensNavigationArgs args))
				this.selected = args.Selected;

			try
			{
				TokensEventArgs e = await this.XmppService.Wallet.GetTokens(0, Constants.Sizes.TokenBatchSize);

				this.UiSerializer.BeginInvokeOnMainThread(() =>
				{
					if (e.Ok)
					{
						this.Tokens.Clear();

						if (!(e.Tokens is null))
						{
							foreach (Token Token in e.Tokens)
								this.Tokens.Add(new TokenItem(Token, this, this.selected));

							this.HasTokens = true;
						}
						else
							this.HasTokens = false;
					}
					else
						this.HasTokens = false;
				});
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <inheritdoc/>
		protected override void XmppService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(e.State);
			});
		}

		#region Properties

		/// <summary>
		/// See <see cref="HasTokens"/>
		/// </summary>
		public static readonly BindableProperty HasTokensProperty =
			BindableProperty.Create(nameof(HasTokens), typeof(bool), typeof(MyTokensViewModel), default(bool));

		/// <summary>
		/// HasTokens of eDaler to process
		/// </summary>
		public bool HasTokens
		{
			get => (bool)this.GetValue(HasTokensProperty);
			set => this.SetValue(HasTokensProperty, value);
		}

		/// <summary>
		/// Holds a list of tokens
		/// </summary>
		public ObservableCollection<TokenItem> Tokens { get; }

		/// <summary>
		/// The command to bind to for returning to previous view.
		/// </summary>
		public ICommand BackCommand { get; }

		/// <summary>
		/// Command executed when more tokens need to be loaded.
		/// </summary>
		public ICommand LoadMoreTokensCommand { get; }

		#endregion

		private async Task GoBack()
		{
			this.selected.TrySetResult(null);
			await this.NavigationService.GoBackAsync();
		}

		private Task LoadMoreTokens()
		{
			return Task.CompletedTask;  // TODO
		}

	}
}