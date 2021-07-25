using System;

namespace Tag.Neuron.Xamarin.Extensions
{
    /// <summary>
    /// An extensions class for the <see cref="TimeSpan"/> struct.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Multiplies the <see cref="TimeSpan"/> instance by the factor specified.
        /// </summary>
        /// <param name="ts">The <see cref="TimeSpan"/> to multiply.</param>
        /// <param name="multiplier">The multiplie to use.</param>
        /// <returns>Scalar multiplication of a timespan.</returns>
        public static TimeSpan Multiply(this TimeSpan ts, int multiplier)
        {
            return TimeSpan.FromTicks(ts.Ticks * multiplier);
        }
    }
}