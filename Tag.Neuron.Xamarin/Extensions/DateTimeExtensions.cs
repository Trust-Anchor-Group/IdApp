﻿using System;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Returns the actual date, if it is non-null, or <c>null</c> if it is <see cref="DateTime.MinValue"/>.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns></returns>
        public static DateTime? GetDateOrNullIfMinValue(this DateTime? date)
        {
            if (!date.HasValue)
                return null;
            return GetDateOrNullIfMinValue(date.Value);
        }

        /// <summary>
        /// Returns the actual date if it has a valid value, or <c>null</c> if it is <see cref="DateTime.MinValue"/>.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns></returns>
        public static DateTime? GetDateOrNullIfMinValue(this DateTime date)
        {
            if (date == DateTime.MinValue)
                return null;
            return date;
        }
    }
}