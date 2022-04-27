using System;

namespace IdApp.Controls.Extended
{
	public class NullableDateChangedEventArgs : EventArgs
	{
		public NullableDateChangedEventArgs(DateTime? oldDate, DateTime? newDate)
		{
			OldDate = oldDate;
			NewDate = newDate;
		}

		public DateTime? NewDate { get; private set; }

		public DateTime? OldDate { get; private set; }
	}
}
