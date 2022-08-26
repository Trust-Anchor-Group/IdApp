using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Operators.Vectors;
using Xamarin.CommunityToolkit.Helpers;
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
			this.Stack = new ObservableCollection<StackItem>();
			this.MemoryItems = new ObservableCollection<object>();

			this.ToggleCommand = new Command(() => this.ExecuteToggle());
			this.ToggleHyperbolicCommand = new Command(() => this.ExecuteToggleHyperbolic());
			this.ToggleInverseCommand = new Command(() => this.ExecuteToggleInverse());
			this.KeyPressCommand = new Command(async (P) => await this.ExecuteKeyPressed(P));
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out CalculatorNavigationArgs args))
			{
				if (!args.ViewInitialized)
				{
					this.Entry = args.Entry;
					this.ViewModel = args.ViewModel;
					this.Property = args.Property;

					if (this.Entry is not null)
						this.Value = this.Entry.Text;
					else if (this.ViewModel is not null && this.Property is not null)
						this.Value = (string)this.ViewModel.GetValue(this.Property);
					else
						this.Value = string.Empty;

					args.ViewInitialized = true;
				}
			}

			this.DecimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
			this.DisplayMain = true;
			this.DisplayFunctions = false;
			this.DisplayHyperbolic = false;
			this.DisplayInverse = false;
			this.DisplayEndParenthesis = false;
			this.DisplayEquals = true;
			this.Status = string.Empty;
			this.Memory = null;
			this.Entering = false;
			this.NrParentheses = 0;
			this.HasValue = !string.IsNullOrEmpty(this.Value);
			this.HasStatistics = false;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			await this.EvaluateStack(true);

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// See <see cref="Value"/>
		/// </summary>
		public static readonly BindableProperty ValueProperty =
			BindableProperty.Create(nameof(Value), typeof(string), typeof(CalculatorViewModel), default(string));

		/// <summary>
		/// Current entry
		/// </summary>
		public string Value
		{
			get => (string)this.GetValue(ValueProperty);
			set
			{
				this.SetValue(ValueProperty, value);
				this.HasValue = !string.IsNullOrEmpty(value);

				if (this.Entry is not null)
					this.Entry.Text = this.Value;

				if (this.ViewModel is not null && this.Property is not null)
					this.ViewModel.SetValue(this.Property, this.Value);
			}
		}

		/// <summary>
		/// See <see cref="Status"/>
		/// </summary>
		public static readonly BindableProperty StatusProperty =
			BindableProperty.Create(nameof(Status), typeof(string), typeof(CalculatorViewModel), default(string));

		/// <summary>
		/// Current entry
		/// </summary>
		public string Status
		{
			get => (string)this.GetValue(StatusProperty);
			set => this.SetValue(StatusProperty, value);
		}

		/// <summary>
		/// See <see cref="Entering"/>
		/// </summary>
		public static readonly BindableProperty EnteringProperty =
			BindableProperty.Create(nameof(Entering), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// Current entry
		/// </summary>
		public bool Entering
		{
			get => (bool)this.GetValue(EnteringProperty);
			set => this.SetValue(EnteringProperty, value);
		}

		/// <summary>
		/// See <see cref="HasValue"/>
		/// </summary>
		public static readonly BindableProperty HasValueProperty =
			BindableProperty.Create(nameof(HasValue), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// If there's a value in the input field
		/// </summary>
		public bool HasValue
		{
			get => (bool)this.GetValue(HasValueProperty);
			set => this.SetValue(HasValueProperty, value);
		}

		/// <summary>
		/// See <see cref="HasStatistics"/>
		/// </summary>
		public static readonly BindableProperty HasStatisticsProperty =
			BindableProperty.Create(nameof(HasStatistics), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// If there's values available for statistical computations.
		/// </summary>
		public bool HasStatistics
		{
			get => (bool)this.GetValue(HasStatisticsProperty);
			set => this.SetValue(HasStatisticsProperty, value);
		}

		/// <summary>
		/// See <see cref="NrParentheses"/>
		/// </summary>
		public static readonly BindableProperty NrParenthesesProperty =
			BindableProperty.Create(nameof(NrParentheses), typeof(int), typeof(CalculatorViewModel), default(int));

		/// <summary>
		/// Number of open parentheses
		/// </summary>
		public int NrParentheses
		{
			get => (int)this.GetValue(NrParenthesesProperty);
			set
			{
				this.SetValue(NrParenthesesProperty, value);
				this.CalcDisplay();
			}
		}

		/// <summary>
		/// See <see cref="Memory"/>
		/// </summary>
		public static readonly BindableProperty MemoryProperty =
			BindableProperty.Create(nameof(Memory), typeof(object), typeof(CalculatorViewModel), default);

		/// <summary>
		/// Current entry
		/// </summary>
		public object Memory
		{
			get => (object)this.GetValue(MemoryProperty);
			set => this.SetValue(MemoryProperty, value);
		}

		/// <summary>
		/// See <see cref="Entry"/>
		/// </summary>
		public static readonly BindableProperty EntryProperty =
			BindableProperty.Create(nameof(Entry), typeof(Entry), typeof(CalculatorViewModel), default(Entry));

		/// <summary>
		/// Entry control, if available
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
		/// View model, if available
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
		/// Property, if available.
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
		/// Current decimal separator.
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
		/// If main key page is to be displayed
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
		/// If function key page is to be displayed
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
		/// If hyperbolic functions are to be displayed
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
		/// If inverse functions are to be displayed
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
		/// If neither hyperbolic nor inverse functions are to be displayed
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
		/// If hyperbolic functions, but not inverse functions are to be displayed
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
		/// If inverse functions, but not hyperbolic functions are to be displayed
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
		/// If inverse hyperbolic functions are to be displayed
		/// </summary>
		public bool DisplayHyperbolicInverse
		{
			get => (bool)this.GetValue(DisplayHyperbolicInverseDisplayHyperbolicInverse);
			set => this.SetValue(DisplayHyperbolicInverseDisplayHyperbolicInverse, value);
		}

		/// <summary>
		/// See <see cref="DisplayEquals"/>
		/// </summary>
		public static readonly BindableProperty DisplayEqualsDisplayEquals =
			BindableProperty.Create(nameof(DisplayEquals), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// If the equals button should be displayed
		/// </summary>
		public bool DisplayEquals
		{
			get => (bool)this.GetValue(DisplayEqualsDisplayEquals);
			set => this.SetValue(DisplayEqualsDisplayEquals, value);
		}

		/// <summary>
		/// See <see cref="DisplayEndParenthesis"/>
		/// </summary>
		public static readonly BindableProperty DisplayEndParenthesisDisplayEndParenthesis =
			BindableProperty.Create(nameof(DisplayEndParenthesis), typeof(bool), typeof(CalculatorViewModel), default(bool));

		/// <summary>
		/// If the end parenthesis button should be displayed.
		/// </summary>
		public bool DisplayEndParenthesis
		{
			get => (bool)this.GetValue(DisplayEndParenthesisDisplayEndParenthesis);
			set => this.SetValue(DisplayEndParenthesisDisplayEndParenthesis, value);
		}

		private void CalcDisplay()
		{
			this.DisplayHyperbolicInverse = this.DisplayFunctions && this.DisplayHyperbolic && this.DisplayInverse;
			this.DisplayNotHyperbolicInverse = this.DisplayFunctions && !this.DisplayHyperbolic && this.DisplayInverse;
			this.DisplayHyperbolicNotInverse = this.DisplayFunctions && this.DisplayHyperbolic && !this.DisplayInverse;
			this.DisplayNotHyperbolicNotInverse = this.DisplayFunctions && !this.DisplayHyperbolic && !this.DisplayInverse;
			this.DisplayEquals = this.DisplayMain && this.NrParentheses == 0;
			this.DisplayEndParenthesis = this.DisplayMain && this.NrParentheses > 0;
		}

		/// <summary>
		/// Holds the contents of the calculation stack
		/// </summary>
		public ObservableCollection<StackItem> Stack { get; }

		/// <summary>
		/// Holds the contents of the memory
		/// </summary>
		public ObservableCollection<object> MemoryItems { get; }

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

		private async Task ExecuteKeyPressed(object P)
		{
			try
			{
				string Key = P?.ToString() ?? string.Empty;

				switch (Key)
				{
					// Key entry

					case "0":
						if (!this.Entering)
							break;

						this.Value += Key;
						break;

					case "1":
					case "2":
					case "3":
					case "4":
					case "5":
					case "6":
					case "7":
					case "8":
					case "9":
						if (!this.Entering)
						{
							this.Value = string.Empty;
							this.Entering = true;
						}

						this.Value += Key;
						break;

					case ".":
						Key = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
						this.Value += Key;
						this.Entering = true;
						break;

					// Results

					case "C":
						this.Value = string.Empty;
						this.Entering = false;
						break;

					case "CE":
						this.Value = string.Empty;
						this.Memory = null;
						this.Stack.Clear();
						this.MemoryItems.Clear();
						this.Entering = false;
						this.HasStatistics = false;

						this.OnPropertyChanged(nameof(this.StackString));
						this.OnPropertyChanged(nameof(this.MemoryString));
						break;

					case "=":
						await this.EvaluateStack();
						break;

					// Unary operators

					case "+-":
						await this.Evaluate("-x");
						break;

					case "1/x":
						await this.Evaluate("1/x");
						break;

					case "%":
						await this.Evaluate("x%");
						break;

					case "%0":
						await this.Evaluate("x‰");
						break;

					case "°":
						await this.Evaluate("x°");
						break;

					case "x2":
						await this.Evaluate("x^2");
						break;

					case "sqrt":
						await this.Evaluate("sqrt(x)");
						break;

					case "10^x":
						await this.Evaluate("10^x");
						break;

					case "2^x":
						await this.Evaluate("2^x");
						break;

					case "rad":
						await this.Evaluate("x*180/pi");
						break;

					// Binary operators

					case "+":
						await this.Evaluate("x+y", "+", OperatorPriority.Terms, false);
						break;

					case "-":
						await this.Evaluate("x-y", "−", OperatorPriority.Terms, false);
						break;

					case "*":
						await this.Evaluate("x*y", "⨉", OperatorPriority.Factors, false);
						break;

					case "/":
						await this.Evaluate("x/y", "÷", OperatorPriority.Factors, false);
						break;

					case "^":
						await this.Evaluate("x^y", "^", OperatorPriority.Powers, false);
						break;

					case "yrt":
						await this.Evaluate("x^(1/y)", "ʸ√", OperatorPriority.Powers, false);
						break;

					// Order

					case "(":

						if (this.Entering)
						{
							await this.Evaluate("x*y", "⨉", OperatorPriority.Factors, true);
							break;
						}

						if (this.Stack.Count > 0)
						{
							this.Stack[^1].StartParenthesis = true;
							this.OnPropertyChanged(nameof(this.StackString));
						}

						break;

					case ")":
						await this.Evaluate(string.Empty, "=", OperatorPriority.Parenthesis, false);
						if (this.Stack.Count > 0)
						{
							this.Stack[^1].StartParenthesis = false;
							this.OnPropertyChanged(nameof(this.StackString));
						}
						break;

					// Analytical Funcions

					case "exp":
					case "lg":
					case "log2":
					case "ln":
					case "sin":
					case "sinh":
					case "asin":
					case "asinh":
					case "cos":
					case "cosh":
					case "acos":
					case "acosh":
					case "tan":
					case "tanh":
					case "atan":
					case "atanh":
					case "sec":
					case "sech":
					case "asec":
					case "asech":
					case "csc":
					case "csch":
					case "acsc":
					case "acsch":
					case "cot":
					case "coth":
					case "acot":
					case "acoth":
						await this.Evaluate(Key + "(x)");
						break;

					// Other scalar functions

					case "abs":
					case "sign":
					case "round":
					case "ceil":
					case "floor":
						await this.Evaluate(Key + "(x)");
						break;

					case "frac":
						await this.Evaluate("x-floor(x)");
						break;

					// Statistics

					case "M+":
						await this.AddToMemory();
						break;

					case "M-":
						await this.SubtractFromMemory();
						break;

					case "MR":
						this.Value = Expression.ToString(this.Memory);
						this.Entering = false;
						break;

					case "avg":
					case "stddev":
					case "sum":
					case "prod":
					case "min":
					case "max":
						await this.EvaluateStatistics(Key + "(x)");
						break;
				}
			}
			catch (Exception ex)
			{
				this.Status = ex.Message;
			}
		}

		private async Task<object> Evaluate()
		{
			if (string.IsNullOrEmpty(this.Value))
				throw new Exception(LocalizationResourceManager.Current["EnterValue"]);

			try
			{
				return await Expression.EvalAsync(this.Value);
			}
			catch (Exception)
			{
				throw new Exception(LocalizationResourceManager.Current["EnterValidValue"]);
			}
		}

		private async Task Evaluate(string Script)
		{
			object x = await this.Evaluate();

			try
			{
				Variables v = new();

				v["x"] = x;

				object y = await Expression.EvalAsync(Script, v);

				this.Value = Expression.ToString(y);
				this.Entering = false;
			}
			catch (Exception)
			{
				throw new Exception(LocalizationResourceManager.Current["CalculationError"]);
			}
		}

		private async Task Evaluate(string Script, string Operator, OperatorPriority Priority, bool StartParenthesis)
		{
			object x = await this.Evaluate();
			StackItem Item;
			int c = this.Stack.Count;

			// if (c > 0 && (Item = this.Stack[c - 1]).StartParenthesis)
			// 	Priority = OperatorPriority.Parenthesis;

			while (c > 0 && (Item = this.Stack[c - 1]).Priority >= Priority && !Item.StartParenthesis)
			{
				object y = x;

				this.Value = Item.Entry;
				x = await this.Evaluate();

				try
				{
					Variables v = new();

					v["x"] = x;
					v["y"] = y;

					x = await Expression.EvalAsync(Item.Script, v);

					this.Value = Expression.ToString(x);
					this.Entering = false;
				}
				catch (Exception)
				{
					throw new Exception(LocalizationResourceManager.Current["CalculationError"]);
				}

				c--;
				this.Stack.RemoveAt(c);
			}

			if (!string.IsNullOrEmpty(Script))
			{
				this.Stack.Add(new StackItem()
				{
					Entry = this.Value,
					Script = Script,
					Operator = Operator,
					Priority = Priority,
					StartParenthesis = StartParenthesis
				});

				this.Value = string.Empty;
			}

			this.Entering = false;
			this.OnPropertyChanged(nameof(this.StackString));
		}

		private async Task EvaluateStatistics(string Script)
		{
			List<IElement> Elements = new();

			foreach (object Item in this.MemoryItems)
				Elements.Add(Expression.Encapsulate(Item));

			try
			{
				Variables v = new();

				v["x"] = VectorDefinition.Encapsulate(Elements, false, null);

				object y = await Expression.EvalAsync(Script, v);

				this.Value = Expression.ToString(y);
				this.Entering = false;
			}
			catch (Exception)
			{
				throw new Exception(LocalizationResourceManager.Current["CalculationError"]);
			}
		}

		/// <summary>
		/// String representation of contents on the stack.
		/// </summary>
		public string StackString
		{
			get
			{
				StringBuilder sb = new();
				bool First = true;
				int NrParantheses = 0;
				OperatorPriority PrevPriority = OperatorPriority.Equals;
				bool StartParenthesis = false;

				foreach (StackItem Item in this.Stack)
				{
					if (First)
						First = false;
					else
						sb.Append(' ');

					if (Item.Priority < PrevPriority || StartParenthesis)
					{
						NrParantheses++;
						sb.Append(" ( ");
					}

					PrevPriority = Item.Priority;
					StartParenthesis = Item.StartParenthesis;

					sb.Append(Item.Entry);
					sb.Append(' ');
					sb.Append(Item.Operator);
				}

				if (StartParenthesis)
				{
					sb.Append(" (");
					NrParantheses++;
				}

				this.NrParentheses = NrParantheses;

				while (NrParantheses > 0)
				{
					sb.Append(" )");
					NrParantheses--;
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// Evaluates the current stack.
		/// </summary>
		public Task EvaluateStack()
		{
			return this.EvaluateStack(false);
		}

		/// <summary>
		/// Evaluates the current stack.
		/// </summary>
		public async Task EvaluateStack(bool IgnoreError)
		{
			if (this.Stack.Count == 0 && string.IsNullOrEmpty(this.Value))
				return;

			if (IgnoreError)
			{
				try
				{
					await this.Evaluate(string.Empty, "=", OperatorPriority.Equals, false);
				}
				catch (Exception)
				{
					// Ignore
				}
			}
			else
				await this.Evaluate(string.Empty, "=", OperatorPriority.Equals, false);
		}

		/// <summary>
		/// String representation of contents on the statistical memory.
		/// </summary>
		public string MemoryString
		{
			get
			{
				if (this.Memory is null)
					return string.Empty;

				StringBuilder sb = new();

				sb.Append("M: ");
				sb.Append(Expression.ToString(this.Memory));
				sb.Append(" (");
				sb.Append(this.MemoryItems.Count.ToString());
				sb.Append(")");

				return sb.ToString();
			}
		}

		private async Task AddToMemory()
		{
			await this.EvaluateStack();

			object x = await this.Evaluate();

			this.MemoryItems.Add(x);

			if (this.Memory is null)
				this.Memory = x;
			else
			{
				Variables v = new();

				v["M"] = this.Memory;
				v["x"] = x;

				this.Memory = await Expression.EvalAsync("M+x", v);
			}

			this.HasStatistics = true;
			this.OnPropertyChanged(nameof(this.MemoryString));
		}

		private async Task SubtractFromMemory()
		{
			await this.EvaluateStack();

			object x = await this.Evaluate();

			this.MemoryItems.Add(x);

			if (this.Memory is null)
			{
				Variables v = new();

				v["x"] = x;

				this.Memory = await Expression.EvalAsync("-x", v);
			}
			else
			{
				Variables v = new();

				v["M"] = this.Memory;
				v["x"] = x;

				this.Memory = await Expression.EvalAsync("M+x", v);
			}

			this.HasStatistics = true;
			this.OnPropertyChanged(nameof(this.MemoryString));
		}

		#endregion
	}
}
