using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Pages.Main.Calculator;
using IdApp.Services.Navigation;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Script.Functions.ComplexNumbers;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.BuyEDaler
{
	/// <summary>
	/// The view model to bind to for buying eDaler.
	/// </summary>
	public class BuyEDalerViewModel : XmppViewModel
	{
		private TaskCompletionSource<decimal?> result;
		private bool buyButtonPressed = false;

		/// <summary>
		/// Creates an instance of the <see cref="BuyEDalerViewModel"/> class.
		/// </summary>
		public BuyEDalerViewModel()
		: base()
		{
			this.BuyCommand = new Command(async _ => await this.Buy(), _ => this.AmountOk);
			this.OpenCalculatorCommand = new Command(async P => await this.OpenCalculator(P));
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out BuyEDalerNavigationArgs Args))
			{
				this.Currency = Args.Currency;
				this.result = Args.Result;
			}

			this.Amount = 0;
			this.AmountText = string.Empty;
			this.AmountOk = false;

			this.AssignProperties();
			this.EvaluateAllCommands();

			this.TagProfile.Changed += this.TagProfile_Changed;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.TagProfile.Changed -= this.TagProfile_Changed;

			this.result?.TrySetResult(this.buyButtonPressed ? this.Amount : null);

			await base.OnDispose();
		}

		private void AssignProperties()
		{
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.BuyCommand, this.OpenCalculatorCommand);
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object Sender, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(NewState);
				this.EvaluateAllCommands();
			});

			return Task.CompletedTask;
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(this.AssignProperties);
		}

		#region Properties

		/// <summary>
		/// See <see cref="Amount"/>
		/// </summary>
		public static readonly BindableProperty AmountProperty =
			BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(BuyEDalerViewModel), default(decimal));

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		public decimal Amount
		{
			get => (decimal)this.GetValue(AmountProperty);
			set => this.SetValue(AmountProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountOk"/>
		/// </summary>
		public static readonly BindableProperty AmountOkProperty =
			BindableProperty.Create(nameof(AmountOk), typeof(bool), typeof(BuyEDalerViewModel), default(bool));

		/// <summary>
		/// If <see cref="Amount"/> is OK.
		/// </summary>
		public bool AmountOk
		{
			get => (bool)this.GetValue(AmountOkProperty);
			set => this.SetValue(AmountOkProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountColor"/>
		/// </summary>
		public static readonly BindableProperty AmountColorProperty =
			BindableProperty.Create(nameof(AmountColor), typeof(Color), typeof(BuyEDalerViewModel), default(Color));

		/// <summary>
		/// Color of <see cref="Amount"/> field.
		/// </summary>
		public Color AmountColor
		{
			get => (Color)this.GetValue(AmountColorProperty);
			set => this.SetValue(AmountColorProperty, value);
		}

		/// <summary>
		/// See <see cref="AmountText"/>
		/// </summary>
		public static readonly BindableProperty AmountTextProperty =
			BindableProperty.Create(nameof(AmountText), typeof(string), typeof(BuyEDalerViewModel), default(string));

		/// <summary>
		/// <see cref="Amount"/> as text.
		/// </summary>
		public string AmountText
		{
			get => (string)this.GetValue(AmountTextProperty);
			set
			{
				this.SetValue(AmountTextProperty, value);

				if (CommonTypes.TryParse(value, out decimal d) && d > 0)
				{
					this.Amount = d;
					this.AmountOk = true;
					this.AmountColor = Color.Default;
				}
				else
				{
					this.AmountOk = false;
					this.AmountColor = Color.Salmon;
				}

				this.EvaluateCommands(this.BuyCommand);
			}
		}

		/// <summary>
		/// See <see cref="Currency"/>
		/// </summary>
		public static readonly BindableProperty CurrencyProperty =
			BindableProperty.Create(nameof(Currency), typeof(string), typeof(BuyEDalerViewModel), default(string));

		/// <summary>
		/// Currency of <see cref="Amount"/>.
		/// </summary>
		public string Currency
		{
			get => (string)this.GetValue(CurrencyProperty);
			set => this.SetValue(CurrencyProperty, value);
		}

		/// <summary>
		/// The command to bind to for buying eDaler.
		/// </summary>
		public ICommand BuyCommand { get; }

		/// <summary>
		/// The command to bind to open the calculator.
		/// </summary>
		public ICommand OpenCalculatorCommand { get; }

		#endregion

		/// <summary>
		/// Opens the calculator for calculating the value of a numerical property.
		/// </summary>
		/// <param name="Parameter">Property to calculate</param>
		public async Task OpenCalculator(object Parameter)
		{
			try
			{
				switch (Parameter?.ToString())
				{
					case "AmountText":
						CalculatorNavigationArgs Args = new(this, AmountTextProperty);

						await this.NavigationService.GoToAsync(nameof(CalculatorPage), Args, BackMethod.Pop);
						break;
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Buy()
		{
			this.buyButtonPressed = true;
			await this.NavigationService.GoBackAsync();
		}

	}
}
