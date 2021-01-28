using System;

namespace Tag.Neuron.Xamarin.Extensions
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Multiply(this TimeSpan ts, int multiplier)
        {
            return TimeSpan.FromTicks(ts.Ticks * multiplier);
        }
    }
}