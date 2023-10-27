using IdApp.Pages.Contracts.NewContract.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Duration
{
	/// <summary>
	/// The view model to bind to for when displaying the duration.
	/// </summary>
	public class DurationViewModel : XmppViewModel
	{
		private bool skipEvaluations = false;

		/// <summary>
		/// Creates an instance of the <see cref="DurationViewModel"/> class.
		/// </summary>
		public DurationViewModel()
			: base()
		{
			this.PlusMinusCommand = new Command(() => this.PlusMinusPressed());
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out DurationNavigationArgs Args))
			{
				this.Entry = Args.Entry;
				this.OnPropertyChanged(nameof(this.Value));
			}

			this.SplitDuration();
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.EvaluateDuration();

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// Current entry
		/// </summary>
		public Waher.Content.Duration Value
		{
			get
			{
				if ((this.Entry is not null) && (this.Entry.BindingContext is ParameterInfo ParameterInfo))
				{
					return ParameterInfo.DurationValue;
				}

				return Waher.Content.Duration.Zero;
			}

			set
			{
				if ((this.Entry is not null) && (this.Entry.BindingContext is ParameterInfo ParameterInfo))
				{
					ParameterInfo.DurationValue = value;
					this.OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// See <see cref="Entry"/>
		/// </summary>
		public static readonly BindableProperty EntryProperty =
			BindableProperty.Create(nameof(Entry), typeof(Entry), typeof(DurationViewModel), default(Entry));

		/// <summary>
		/// Entry control, if available
		/// </summary>
		public Entry Entry
		{
			get => (Entry)this.GetValue(EntryProperty);
			set => this.SetValue(EntryProperty, value);
		}

		/// <summary>
		/// See <see cref="IsNegativeDuration"/>
		/// </summary>
		public static readonly BindableProperty IsNegativeDurationProperty =
			BindableProperty.Create(nameof(IsNegativeDuration), typeof(bool), typeof(DurationViewModel), false);

		/// <summary>
		/// IsNegativeDuration
		/// </summary>
		public bool IsNegativeDuration
		{
			get => (bool)this.GetValue(IsNegativeDurationProperty);
			set
			{
				this.SetValue(IsNegativeDurationProperty, value);
				this.EvaluateDuration();
			}
		}

		/// <summary>
		/// See <see cref="Years"/>
		/// </summary>
		public static readonly BindableProperty YearsProperty =
			BindableProperty.Create(nameof(Years), typeof(string), typeof(DurationViewModel), default(string));

		/// <summary>
		/// Years
		/// </summary>
		public string Years
		{
			get => (string)this.GetValue(YearsProperty);
			set
			{
				this.SetValue(YearsProperty, value);
				this.EvaluateDuration();
			}
		}

		/// <summary>
		/// See <see cref="Months"/>
		/// </summary>
		public static readonly BindableProperty MonthsProperty =
			BindableProperty.Create(nameof(Months), typeof(string), typeof(DurationViewModel), default(string));

		/// <summary>
		/// Months
		/// </summary>
		public string Months
		{
			get => (string)this.GetValue(MonthsProperty);
			set
			{
				this.SetValue(MonthsProperty, value);
				this.EvaluateDuration();
			}
		}

		/// <summary>
		/// See <see cref="Days"/>
		/// </summary>
		public static readonly BindableProperty DaysProperty =
			BindableProperty.Create(nameof(Days), typeof(string), typeof(DurationViewModel), default(string));

		/// <summary>
		/// Days
		/// </summary>
		public string Days
		{
			get => (string)this.GetValue(DaysProperty);
			set
			{
				this.SetValue(DaysProperty, value);
				this.EvaluateDuration();
			}
		}

		/// <summary>
		/// See <see cref="Hours"/>
		/// </summary>
		public static readonly BindableProperty HoursProperty =
			BindableProperty.Create(nameof(Hours), typeof(string), typeof(DurationViewModel), default(string));

		/// <summary>
		/// Hours
		/// </summary>
		public string Hours
		{
			get => (string)this.GetValue(HoursProperty);
			set
			{
				this.SetValue(HoursProperty, value);
				this.EvaluateDuration();
			}
		}

		/// <summary>
		/// See <see cref="Minutes"/>
		/// </summary>
		public static readonly BindableProperty MinutesProperty =
			BindableProperty.Create(nameof(Minutes), typeof(string), typeof(DurationViewModel), default(string));

		/// <summary>
		/// Minutes
		/// </summary>
		public string Minutes
		{
			get => (string)this.GetValue(MinutesProperty);
			set
			{
				this.SetValue(MinutesProperty, value);
				this.EvaluateDuration();
			}
		}

		/// <summary>
		/// See <see cref="Seconds"/>
		/// </summary>
		public static readonly BindableProperty SecondsProperty =
			BindableProperty.Create(nameof(Seconds), typeof(string), typeof(DurationViewModel), default(string));

		/// <summary>
		/// Seconds
		/// </summary>
		public string Seconds
		{
			get => (string)this.GetValue(SecondsProperty);
			set
			{
				this.SetValue(SecondsProperty, value);
				this.EvaluateDuration();
			}
		}

		#endregion

		#region Commands

		/// <summary>
		/// Command executed when user presses a key button.
		/// </summary>
		public ICommand PlusMinusCommand { get; }

		private void PlusMinusPressed()
		{
			this.IsNegativeDuration = !this.IsNegativeDuration;
			this.EvaluateDuration();
		}

		private bool TryGetValue(string Duration, [NotNullWhen(true)] out string ValueString, [NotNullWhen(true)] out string ValueUnit)
		{
			ValueString = string.Empty;
			ValueUnit = string.Empty;

			for (int i = 0; i < Duration.Length; i++)
			{
				char ch = Duration[i];

				if ("0123456789.".Contains(ch))
				{
					ValueString += ch;
				}
				else if ("YMDHS".Contains(ch))
				{
					ValueUnit += ch;
					break;
				}
			}

			return (ValueString.Length > 0) && (ValueUnit.Length > 0);
		}

		private bool ParseAnInt(string Text)
		{
			try
			{
				int Parsed = int.Parse(Text);
			}
			catch
			{
				return false;
			}

			return true;
		}

		private bool ParseAnFloat(string Text)
		{
			try
			{
				float Parsed = float.Parse(Text, NumberStyles.AllowDecimalPoint);
			}
			catch
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Split the current duration value into components.
		/// </summary>
		private void SplitDuration()
		{
			this.skipEvaluations = true;

			this.IsNegativeDuration = this.Value.Negation;
			this.Years = this.Value.Years.ToString(CultureInfo.InvariantCulture);
			this.Months = this.Value.Months.ToString(CultureInfo.InvariantCulture);
			this.Days = this.Value.Days.ToString(CultureInfo.InvariantCulture);
			this.Hours = this.Value.Hours.ToString(CultureInfo.InvariantCulture);
			this.Minutes = this.Value.Minutes.ToString(CultureInfo.InvariantCulture);

			if (this.Value.Seconds > 0)
			{
				this.Seconds = this.Value.Seconds.ToString(CultureInfo.InvariantCulture);
			}

			this.skipEvaluations = false;
		}

		/// <summary>
		/// Evaluates the current duration.
		/// </summary>
		private void EvaluateDuration()
		{
			if (this.skipEvaluations)
			{
				return;
			}

			bool IsZero = true;
			StringBuilder sb = new();

			sb.Append(this.IsNegativeDuration ? "-P" : "P");

			if (!string.IsNullOrEmpty(this.Years))
			{
				IsZero = false;
				sb.Append(this.Years);
				sb.Append("Y");
			}

			if (!string.IsNullOrEmpty(this.Months))
			{
				IsZero = false;
				sb.Append(this.Months);
				sb.Append("M");
			}

			if (!string.IsNullOrEmpty(this.Days))
			{
				IsZero = false;
				sb.Append(this.Days);
				sb.Append("D");
			}

			bool IsHours = !string.IsNullOrEmpty(this.Hours);
			bool IsMinutes = !string.IsNullOrEmpty(this.Minutes);
			bool IsSeconds = !string.IsNullOrEmpty(this.Seconds);

			if (IsHours || IsMinutes || IsSeconds)
			{
				IsZero = false;
				sb.Append("T");

				if (IsHours)
				{
					sb.Append(this.Hours);
					sb.Append("H");
				}

				if (IsMinutes)
				{
					sb.Append(this.Minutes);
					sb.Append("M");
				}

				if (IsSeconds)
				{
					sb.Append(this.Seconds);
					sb.Append("S");
				}
			}

			if (IsZero)
			{
				sb.Append("0D");
			}

			if (Waher.Content.Duration.TryParse(sb.ToString(), out Waher.Content.Duration Result))
			{
				this.Value = Result;
			}
		}

		#endregion
	}
}
