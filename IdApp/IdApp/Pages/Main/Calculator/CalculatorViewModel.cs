using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Calculator
{
	/// <summary>
	/// The view model to bind to for when displaying the calculator.
	/// </summary>
	public class CalculatorViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="CalculatorViewModel"/> class.
		/// </summary>
		public CalculatorViewModel()
			: base()
		{
			this.ToggleCommand = new Command(() => this.ExecuteToggle());
			this.ToggleHyperbolicCommand = new Command(() => this.ExecuteToggleHyperbolic());
			this.ToggleInverseCommand = new Command(() => this.ExecuteToggleInverse());
			this.KeyPressCommand = new Command((P) => this.ExecuteKeyPressed(P));
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out CalculatorNavigationArgs args))
			{
				this.Entry = args.Entry;
				this.ViewModel = args.ViewModel;
				this.Property = args.Property;
			}

			this.DecimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
			this.DisplayMain = true;
			this.DisplayFunctions = false;
			this.DisplayHyperbolic = false;
			this.DisplayInverse = false;
		}

		#region Properties

		/// <summary>
		/// See <see cref="Entry"/>
		/// </summary>
		public static readonly BindableProperty EntryProperty =
			BindableProperty.Create(nameof(Entry), typeof(Entry), typeof(CalculatorViewModel), default(Entry));

		/// <summary>
		/// Entry of eDaler
		/// </summary>
		public Entry Entry
		{
			get => (Entry)this.GetValue(EntryProperty);
			set => this.SetValue(EntryProperty, value);
		}

		/// <summary>
		/// See <see cref="ViewModel"/>
		/// </summary>
		public static readonly BindableProperty ViewModelProperty =
			BindableProperty.Create(nameof(ViewModel), typeof(BaseViewModel), typeof(CalculatorViewModel), default(BaseViewModel));

		/// <summary>
		/// ViewModel of eDaler
		/// </summary>
		public BaseViewModel ViewModel
		{
			get => (BaseViewModel)this.GetValue(ViewModelProperty);
			set => this.SetValue(ViewModelProperty, value);
		}

		/// <summary>
		/// See <see cref="Property"/>
		/// </summary>
		public static readonly BindableProperty PropertyProperty =
			BindableProperty.Create(nameof(Property), typeof(BindableProperty), typeof(CalculatorViewModel), default(BindableProperty));

		/// <summary>
		/// Property of eDaler
		/// </summary>
		public BindableProperty Property
		{
			get => (BindableProperty)this.GetValue(PropertyProperty);
			set => this.SetValue(PropertyProperty, value);
		}

		/// <summary>
		/// See <see cref="DecimalSeparator"/>
		/// </summary>
		public static readonly BindableProperty DecimalSeparatorDecimalSeparator =
			BindableProperty.Create(nameof(DecimalSeparator), typeof(string), typeof(CalculatorViewModel), default(string));

		/// <summary>
		/// DecimalSeparator of eDaler
		/// </summary>
		public string DecimalSeparator
		{
			get => (string)this.GetValue(DecimalSeparatorDecimalSeparator);
			set => this.SetValue(DecimalSeparatorDecimalSeparator, value);
		}

		/// <summary>
		/// See <see cref="DisplayMain"/>
		/// </summary>
		public static readonly BindableProperty DisplayMainDisplayMain =
			BindableProperty.Create(nameof(DisplayMain), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// DisplayMain of eDaler
		/// </summary>
		public bool DisplayMain
		{
			get => (bool)this.GetValue(DisplayMainDisplayMain);
			set => this.SetValue(DisplayMainDisplayMain, value);
		}

		/// <summary>
		/// See <see cref="DisplayFunctions"/>
		/// </summary>
		public static readonly BindableProperty DisplayFunctionsDisplayFunctions =
			BindableProperty.Create(nameof(DisplayFunctions), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// DisplayFunctions of eDaler
		/// </summary>
		public bool DisplayFunctions
		{
			get => (bool)this.GetValue(DisplayFunctionsDisplayFunctions);
			set
			{
				this.SetValue(DisplayFunctionsDisplayFunctions, value);
				this.CalcDisplay();
			}
		}

		/// <summary>
		/// See <see cref="DisplayHyperbolic"/>
		/// </summary>
		public static readonly BindableProperty DisplayHyperbolicDisplayHyperbolic =
			BindableProperty.Create(nameof(DisplayHyperbolic), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// DisplayHyperbolic of eDaler
		/// </summary>
		public bool DisplayHyperbolic
		{
			get => (bool)this.GetValue(DisplayHyperbolicDisplayHyperbolic);
			set
			{
				this.SetValue(DisplayHyperbolicDisplayHyperbolic, value);
				this.CalcDisplay();
			}
		}

		/// <summary>
		/// See <see cref="DisplayInverse"/>
		/// </summary>
		public static readonly BindableProperty DisplayInverseDisplayInverse =
			BindableProperty.Create(nameof(DisplayInverse), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// DisplayInverse of eDaler
		/// </summary>
		public bool DisplayInverse
		{
			get => (bool)this.GetValue(DisplayInverseDisplayInverse);
			set
			{
				this.SetValue(DisplayInverseDisplayInverse, value);
				this.CalcDisplay();
			}
		}

		/// <summary>
		/// See <see cref="DisplayNotHyperbolicNotInverse"/>
		/// </summary>
		public static readonly BindableProperty DisplayNotHyperbolicNotInverseDisplayNotHyperbolicNotInverse =
			BindableProperty.Create(nameof(DisplayNotHyperbolicNotInverse), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// DisplayNotHyperbolicNotInverse of eDaler
		/// </summary>
		public bool DisplayNotHyperbolicNotInverse
		{
			get => (bool)this.GetValue(DisplayNotHyperbolicNotInverseDisplayNotHyperbolicNotInverse);
			set => this.SetValue(DisplayNotHyperbolicNotInverseDisplayNotHyperbolicNotInverse, value);
		}

		/// <summary>
		/// See <see cref="DisplayHyperbolicNotInverse"/>
		/// </summary>
		public static readonly BindableProperty DisplayHyperbolicNotInverseDisplayHyperbolicNotInverse =
			BindableProperty.Create(nameof(DisplayHyperbolicNotInverse), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// DisplayHyperbolicNotInverse of eDaler
		/// </summary>
		public bool DisplayHyperbolicNotInverse
		{
			get => (bool)this.GetValue(DisplayHyperbolicNotInverseDisplayHyperbolicNotInverse);
			set => this.SetValue(DisplayHyperbolicNotInverseDisplayHyperbolicNotInverse, value);
		}

		/// <summary>
		/// See <see cref="DisplayNotHyperbolicInverse"/>
		/// </summary>
		public static readonly BindableProperty DisplayNotHyperbolicInverseDisplayNotHyperbolicInverse =
			BindableProperty.Create(nameof(DisplayNotHyperbolicInverse), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// DisplayNotHyperbolicInverse of eDaler
		/// </summary>
		public bool DisplayNotHyperbolicInverse
		{
			get => (bool)this.GetValue(DisplayNotHyperbolicInverseDisplayNotHyperbolicInverse);
			set => this.SetValue(DisplayNotHyperbolicInverseDisplayNotHyperbolicInverse, value);
		}

		/// <summary>
		/// See <see cref="DisplayHyperbolicInverse"/>
		/// </summary>
		public static readonly BindableProperty DisplayHyperbolicInverseDisplayHyperbolicInverse =
			BindableProperty.Create(nameof(DisplayHyperbolicInverse), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// DisplayHyperbolicInverse of eDaler
		/// </summary>
		public bool DisplayHyperbolicInverse
		{
			get => (bool)this.GetValue(DisplayHyperbolicInverseDisplayHyperbolicInverse);
			set => this.SetValue(DisplayHyperbolicInverseDisplayHyperbolicInverse, value);
		}

		private void CalcDisplay()
		{
			this.DisplayHyperbolicInverse = this.DisplayFunctions && this.DisplayHyperbolic && this.DisplayInverse;
			this.DisplayNotHyperbolicInverse = this.DisplayFunctions && !this.DisplayHyperbolic && this.DisplayInverse;
			this.DisplayHyperbolicNotInverse = this.DisplayFunctions && this.DisplayHyperbolic && !this.DisplayInverse;
			this.DisplayNotHyperbolicNotInverse = this.DisplayFunctions && !this.DisplayHyperbolic && !this.DisplayInverse;
		}

		#endregion

		#region Commands

		/// <summary>
		/// Command executed when user wants to toggle buttons.
		/// </summary>
		public ICommand ToggleCommand { get; }

		private void ExecuteToggle()
		{
			this.DisplayMain = !this.DisplayMain;
			this.DisplayFunctions = !this.DisplayFunctions;
		}

		/// <summary>
		/// Command executed when user wants to toggle hyperbolic functions on/off.
		/// </summary>
		public ICommand ToggleHyperbolicCommand { get; }

		private void ExecuteToggleHyperbolic()
		{
			this.DisplayHyperbolic = !this.DisplayHyperbolic;
		}

		/// <summary>
		/// Command executed when user wants to toggle inverse functions on/off.
		/// </summary>
		public ICommand ToggleInverseCommand { get; }

		private void ExecuteToggleInverse()
		{
			this.DisplayInverse = !this.DisplayInverse;
		}

		/// <summary>
		/// Command executed when user presses a key buttonn.
		/// </summary>
		public ICommand KeyPressCommand { get; }

		private void ExecuteKeyPressed(object P)
		{
			string Key = P?.ToString() ?? string.Empty;

			switch (Key)
			{
				case "0": break;
				case "1": break;
				case "2": break;
				case "3": break;
				case "4": break;
				case "5": break;
				case "6": break;
				case "7": break;
				case "8": break;
				case "9": break;
				case "+": break;
				case "M+": break;
				case "-": break;
				case "M-": break;
				case "*": break;
				case "MR": break;
				case "+-": break;
				case "/": break;
				case "C": break;
				case "1/x": break;
				case "%": break;
				case "%0": break;
				case "°": break;
				case "x2": break;
				case "^": break;
				case "CE": break;
				case "()": break;
				case "sqrt": break;
				case "yrt": break;
				case ".": break;
				case "exp": break;
				case "sin": break;
				case "sinh": break;
				case "asin": break;
				case "asinh": break;
				case "abs": break;
				case "10^x": break;
				case "2^x": break;
				case "cos": break;
				case "cosh": break;
				case "acos": break;
				case "acosh": break;
				case "frac": break;
				case "log": break;
				case "lg2": break;
				case "tan": break;
				case "tanh": break;
				case "atan": break;
				case "atanh": break;
				case "sign": break;
				case "ln": break;
				case "sec": break;
				case "sech": break;
				case "asec": break;
				case "asech": break;
				case "round": break;
				case "csc": break;
				case "csch": break;
				case "acsc": break;
				case "acsch": break;
				case "ceil": break;
				case "cot": break;
				case "coth": break;
				case "acot": break;
				case "acoth": break;
				case "floor": break;
				case "rad": break;
				case "E": break;
				case "stddev": break;
				case "sum": break;
				case "prod": break;
				case "inf": break;
				case "sup": break;
				case "=": break;
			}
		}

		#endregion


	}
}
