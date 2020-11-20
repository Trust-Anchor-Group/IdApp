using System;
using NUnit.Framework;
using XamarinApp.ViewModels;

namespace XamarinApp.Tests.Extensions
{
    public static class ViewModelTestsExtensions
    {
        public static T And<T>(this T vm, Action<T> action) where T : BaseViewModel
        {
            action(vm);
            return vm;
        }

        public static void ThenAssert<T>(this T vm, Func<T, bool> func) where T : BaseViewModel
        {
            Assert.True(func(vm));
        }
    }
}
