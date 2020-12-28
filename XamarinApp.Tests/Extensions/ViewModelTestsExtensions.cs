using System;
using NUnit.Framework;
using Tag.Sdk.UI.ViewModels;

namespace XamarinApp.Tests.Extensions
{
    public static class ViewModelTestsExtensions
    {
        public static T And<T>(this T vm, Action<T> action) where T : BaseViewModel
        {
            action(vm);
            return vm;
        }

        public static T ThenAssert<T>(this T vm, Func<T, bool> func) where T : BaseViewModel
        {
            Assert.True(func(vm));
            return vm;
        }

        public static T ThenAssert<T>(this T vm, Action action) where T : BaseViewModel
        {
            action();
            return vm;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static void Finally<T>(this T vm, Action action) where T : BaseViewModel
#pragma warning restore IDE0060 // Remove unused parameter
        {
            action();
        }
    }
}
