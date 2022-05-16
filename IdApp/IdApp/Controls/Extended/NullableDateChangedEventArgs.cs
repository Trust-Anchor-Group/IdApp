using System;

namespace IdApp.Controls.Extended
{
	/// <summary>
	/// An ExtendedDatePicker event args .
	/// </summary>
	public class NullableDateChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="NullableDateChangedEventArgs"/> class.
		/// </summary>
		/// <param name="oldDate">The previous date</param>
		/// <param name="newDate">The selected date</param>
		public NullableDateChangedEventArgs(DateTime? oldDate, DateTime? newDate)
		{
			OldDate = oldDate;
			NewDate = newDate;
		}

		/// <summary>
		/// The previous date
		/// </summary>
		public DateTime? NewDate { get; private set; }

		/// <summary>
		/// The selected date
		/// </summary>
		public DateTime? OldDate { get; private set; }
	}
}
