using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Main.Duration
{
	/// <summary>
	/// A page that allows the user to duration the value of a numerical input field.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class DurationPage
	{
		/// <summary>
		/// A page that allows the user to duration the value of a numerical input field.
		/// </summary>
		public DurationPage()
		{
			this.ViewModel = new DurationViewModel();

			this.InitializeComponent();

			this.EntryYears.Text = string.Empty;
			this.EntryMonths.Text = string.Empty;
			this.EntryDays.Text = string.Empty;
			this.EntryHours.Text = string.Empty;
			this.EntryMinutes.Text = string.Empty;
			this.EntrySeconds.Text = string.Empty;
		}

		void TrimZeros(Entry Entry, TextChangedEventArgs e)
		{
			string Text = Entry.Text;

			if (string.IsNullOrEmpty(Text))
			{
				return;
			}

			try
			{
				int Parsed = int.Parse(Text);

				if (Parsed == 0)
				{
					Entry.Text = e.OldTextValue;
				}
				else
				{
					Entry.Text = Parsed.ToString();
				}
			}
			catch
			{
				Entry.Text = e.OldTextValue;
			}
		}

		private void DefaultTextChanged(Entry Entry, TextChangedEventArgs e)
		{
			string NewText = e.NewTextValue;
			bool IsValid = NewText.ToCharArray().All(ch => "0123456789".Contains(ch));

			if (!IsValid)
			{
				Entry.Text = e.OldTextValue;
				return;
			}

			this.TrimZeros(Entry, e);
		}

		private void EntryYears_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.DefaultTextChanged(this.EntryYears, e);
        }

		private void EntryMonths_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.DefaultTextChanged(this.EntryMonths, e);
		}

		private void EntryDays_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.DefaultTextChanged(this.EntryDays, e);
		}

		private void EntryHours_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.DefaultTextChanged(this.EntryHours, e);
		}

		private void EntryMinutes_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.DefaultTextChanged(this.EntryMinutes, e);
		}

		private void EntrySeconds_TextChanged(object sender, TextChangedEventArgs e)
		{
			string NewText = e.NewTextValue;
			bool IsValid = NewText.ToCharArray().All(ch => "0123456789.".Contains(ch));

			if (!IsValid)
			{
				this.EntrySeconds.Text = e.OldTextValue;
				return;
			}

			//!!! this.TrimZeros(this.EntrySeconds, e, true);
		}
	}
}
