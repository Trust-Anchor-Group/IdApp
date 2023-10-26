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
				this.ViewModel = Args.ViewModel;
				this.Property = Args.Property;

				if (this.Entry is not null)
					this.Value = this.Entry.Text;
				else if (this.ViewModel is not null && this.Property is not null)
					this.Value = (string)this.ViewModel.GetValue(this.Property);
				else
					this.Value = string.Empty;

			}

			this.EvaluateDuration();
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.EvaluateDuration();

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// See <see cref="Value"/>
		/// </summary>
		public static readonly BindableProperty ValueProperty =
			BindableProperty.Create(nameof(Value), typeof(string), typeof(DurationViewModel), default(string));

		/// <summary>
		/// Current entry
		/// </summary>
		public string Value
		{
			get => (string)this.GetValue(ValueProperty);
			set
			{
				this.SetValue(ValueProperty, value);

				if (this.Entry is not null)
					this.Entry.Text = this.Value;

				if (this.ViewModel is not null && this.Property is not null)
					this.ViewModel.SetValue(this.Property, this.Value);
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
		/// See <see cref="ViewModel"/>
		/// </summary>
		public static readonly BindableProperty ViewModelProperty =
			BindableProperty.Create(nameof(ViewModel), typeof(BaseViewModel), typeof(DurationViewModel), default(BaseViewModel));

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
			BindableProperty.Create(nameof(Property), typeof(BindableProperty), typeof(DurationViewModel), default(BindableProperty));

		/// <summary>
		/// Property, if available.
		/// </summary>
		public BindableProperty Property
		{
			get => (BindableProperty)this.GetValue(PropertyProperty);
			set => this.SetValue(PropertyProperty, value);
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

		/// <summary>
		/// Evaluates the current duration.
		/// </summary>
		public void EvaluateDuration()
		{
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

			this.Value = sb.ToString();

			return;
		}

		#endregion
	}
}
