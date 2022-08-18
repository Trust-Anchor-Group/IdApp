using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Pages.Wallet.MyWallet.ObjectModels;
using IdApp.Services.Notification;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using Waher.Persistence;
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
			this.BackCommand = new Command(async _ => await this.GoBack());
			this.LoadMoreTokensCommand = new Command(async _ => await this.LoadMoreTokens());

			this.Tokens = new ObservableCollection<TokenItem>();
		}

		/// <inheritdoc/>
		public override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out MyTokensNavigationArgs args))
				this.selected = args.Selected;

			try
			{
				TokensEventArgs e = await this.XmppService.Wallet.GetTokens(0, Constants.BatchSizes.TokenBatchSize);
				SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCateogy = this.NotificationService.GetEventsByCategory(EventButton.Wallet);

				this.UiSerializer.BeginInvokeOnMainThread(() =>
				{
					if (e.Ok)
					{
						this.Tokens.Clear();

						if (!(e.Tokens is null))
						{
							foreach (Token Token in e.Tokens)
							{
								if (!EventsByCateogy.TryGetValue(Token.TokenId, out NotificationEvent[] Events))
									Events = new NotificationEvent[0];

								this.Tokens.Add(new TokenItem(Token, this, this.selected, Events));
							}

							this.HasTokens = true;
							this.HasMoreTokens = e.Tokens.Length == Constants.BatchSizes.TokenBatchSize;
						}
						else
							this.HasTokens = false;
					}
					else
						this.HasTokens = false;
				});

				this.XmppService.Wallet.TokenAdded += this.Wallet_TokenAdded;
				this.XmppService.Wallet.TokenRemoved += this.Wallet_TokenRemoved;
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <inheritdoc/>
		public override Task OnDispose()
		{
			this.XmppService.Wallet.TokenAdded -= this.Wallet_TokenAdded;
			this.XmppService.Wallet.TokenRemoved -= this.Wallet_TokenRemoved;

			this.selected?.TrySetResult(null);

			return base.OnDispose();
		}

		private Task Wallet_TokenAdded(object Sender, TokenEventArgs e)
		{
			if (!this.NotificationService.TryGetNotificationEvents(EventButton.Wallet, e.Token.TokenId, out NotificationEvent[] Events))
				Events = new NotificationEvent[0];

			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				TokenItem Item = new(e.Token, this, this.selected, Events);

				if (this.Tokens.Count == 0)
					this.Tokens.Add(Item);
				else
					this.Tokens.Insert(0, Item);
			});

			return Task.CompletedTask;
		}

		private Task Wallet_TokenRemoved(object Sender, TokenEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				int i, c = this.Tokens.Count;

				for (i = 0; i < c; i++)
				{
					TokenItem Item = this.Tokens[i];

					if (Item.TokenId == e.Token.TokenId)
					{
						this.Tokens.RemoveAt(i);
						break;
					}
				}
			});

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override void XmppService_ConnectionStateChanged(object Sender, ConnectionStateChangedEventArgs e)
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
		/// See <see cref="HasMoreTokens"/>
		/// </summary>
		public static readonly BindableProperty HasMoreTokensProperty =
			BindableProperty.Create(nameof(HasMoreTokens), typeof(bool), typeof(MyTokensViewModel), default(bool));

		/// <summary>
		/// HasMoreTokens of eDaler to process
		/// </summary>
		public bool HasMoreTokens
		{
			get => (bool)this.GetValue(HasMoreTokensProperty);
			set => this.SetValue(HasMoreTokensProperty, value);
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

		private async Task LoadMoreTokens()
		{
			if (this.HasMoreTokens)
			{
				this.HasMoreTokens = false; // So multiple requests are not made while scrolling.

				try
				{
					TokensEventArgs e = await this.XmppService.Wallet.GetTokens(this.Tokens.Count, Constants.BatchSizes.TokenBatchSize);
					SortedDictionary<CaseInsensitiveString, NotificationEvent[]> EventsByCateogy = this.NotificationService.GetEventsByCategory(EventButton.Wallet);

					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						if (e.Ok)
						{
							if (!(e.Tokens is null))
							{
								foreach (Token Token in e.Tokens)
								{
									if (!EventsByCateogy.TryGetValue(Token.TokenId, out NotificationEvent[] Events))
										Events = new NotificationEvent[0];

									this.Tokens.Add(new TokenItem(Token, this, Events));
								}

								this.HasMoreTokens = e.Tokens.Length == Constants.BatchSizes.TokenBatchSize;
							}
						}
					});
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
				}
			}
		}

	}
}
