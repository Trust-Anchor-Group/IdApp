using System;
using NUnit.Framework;

namespace XamarinApp.Tests.Extensions
{
    public static class ViewModelTestsExtensions
    {
        public static T And<T>(this T vm, Action<T> action)
        {
            action(vm);
            return vm;
        }

        public static void ThenAssert<T>(this T vm, Func<T, bool> func)
        {
            Assert.True(func(vm));
        }
    }
}
