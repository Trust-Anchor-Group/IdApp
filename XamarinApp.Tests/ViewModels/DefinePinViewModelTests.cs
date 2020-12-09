﻿using Moq;
using XamarinApp.Services;
using XamarinApp.Tests.Extensions;
using XamarinApp.ViewModels.Registration;
using NUnit.Framework;

namespace XamarinApp.Tests.ViewModels
{
    public class DefinePinViewModelTests : ViewModelTests<DefinePinViewModel>
    {
        private readonly Mock<INeuronService> neuronService = new Mock<INeuronService>();
        private readonly Mock<INavigationService> navigationService = new Mock<INavigationService>();
        private readonly Mock<ISettingsService> settingsService = new Mock<ISettingsService>();
        private readonly Mock<ILogService> logService = new Mock<ILogService>();
        
        protected override DefinePinViewModel AViewModel()
        {
            return new DefinePinViewModel(new TagProfile(), neuronService.Object, navigationService.Object, this.settingsService.Object, this.logService.Object);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void When_ContinueIsClicked_AndPinIsEmpty_ThenAWarningIsDisplayed(string pin)
        {
            Given(AViewModel)
                .And(vm => vm.Pin = pin)
                .And(vm => vm.ContinueCommand.IsExecuted())
                .ThenAssert(() => navigationService.Verify(x => x.DisplayAlert(AppResources.ErrorTitle, string.Format(AppResources.PinTooShort, Constants.Authentication.MinPinLength)), Times.Once))
                .Finally(() => navigationService.Reset());
        }

        [Test]
        [TestCase("abcdefgh ")]
        [TestCase(" abcdefgh")]
        public void When_ContinueIsClicked_AndPinContainsWhitespace_ThenAWarningIsDisplayed(string pin)
        {
            Given(AViewModel)
                .And(vm => vm.Pin = pin)
                .And(vm => vm.ContinueCommand.IsExecuted())
                .ThenAssert(() => navigationService.Verify(x => x.DisplayAlert(AppResources.ErrorTitle, AppResources.PinMustNotIncludeWhitespace), Times.Once))
                .ThenAssert(vm => vm.UsePin)
                .Finally(() => navigationService.Reset());
        }

        [Test]
        public void When_SkipIsClicked_ThenUsePinIsFalse()
        {
            Given(AViewModel)
                .And(vm => vm.SkipCommand.IsExecuted())
                .ThenAssert(vm => !vm.UsePin);
        }

        [Test]
        [TestCase("", "123", true)]
        [TestCase("123", "", true)]
        [TestCase("123", "123", true)]
        [TestCase("12345678", "123", false)]
        [TestCase("12345678", "12345678", false)]
        public void PinIsTooShort_IsSet(string pin, string retypedPin, bool expected)
        {
            Given(AViewModel)
                .And(vm => vm.Pin = pin)
                .And(vm => vm.RetypedPin = retypedPin)
                .ThenAssert(vm => vm.PinIsTooShort == expected);
        }

        [Test]
        [TestCase("", "123", false)]
        [TestCase("123", "", false)]
        [TestCase("123", "12", true)]
        [TestCase("123", "123", false)]
        [TestCase("12345678", "123", true)]
        public void PinDoNotMatch_IsSet(string pin, string retypedPin, bool expected)
        {
            Given(AViewModel)
                .And(vm => vm.Pin = pin)
                .And(vm => vm.RetypedPin = retypedPin)
                .ThenAssert(vm => vm.PinsDoNotMatch == expected);
        }
    }
}
