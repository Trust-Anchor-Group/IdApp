using System;

namespace XamarinApp.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime? GetDateOrNullIfMinValue(this DateTime? date)
        {
            if (!date.HasValue)
                return null;
            return GetDateOrNullIfMinValue(date.Value);
        }

        public static DateTime? GetDateOrNullIfMinValue(this DateTime date)
        {
            if (date == DateTime.MinValue)
                return null;
            return date;
        }
    }
}