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
					Entry.Text = e.OldTextValue ?? string.Empty;
				}
				else
				{
					Entry.Text = Parsed.ToString();
				}
			}
			catch
			{
				Entry.Text = e.OldTextValue ?? string.Empty;
			}
		}

		private void DefaultTextChanged(Entry Entry, TextChangedEventArgs e)
		{
			string NewText = e.NewTextValue;

			if (string.IsNullOrEmpty(NewText))
			{
				return;
			}

			bool IsValid = NewText.ToCharArray().All(ch => "0123456789".Contains(ch));

			if (!IsValid)
			{
				Entry.Text = e.OldTextValue ?? string.Empty;
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

			if (string.IsNullOrEmpty(NewText))
			{
				return;
			}

			bool IsValid = NewText.ToCharArray().All(ch => "0123456789.".Contains(ch));

			if (!IsValid)
			{
				this.EntrySeconds.Text = e.OldTextValue ?? string.Empty;
				return;
			}

			// trim leading zeros
			while ((NewText.Length > 1) && (NewText[0] == '0') && (NewText[1] != '.'))
			{
				NewText = NewText[1..];
			}

			this.EntrySeconds.Text = NewText;
		}
	}
}
