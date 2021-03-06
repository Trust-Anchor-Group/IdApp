﻿using IdApp.ViewModels.Registration;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Tests.Extensions;
using Tag.Neuron.Xamarin.UI.Tests.ViewModels;

namespace IdApp.Tests.ViewModels.Registration
{
    public class ChooseAccountViewModelTests : ViewModelTests<ChooseAccountViewModel>
    {
        private readonly Mock<IUiDispatcher> dispatcher = new Mock<IUiDispatcher>();
        private readonly Mock<INeuronService> neuronService = new Mock<INeuronService>();
        private readonly Mock<INavigationService> navigationService = new Mock<INavigationService>();
        private readonly Mock<ICryptoService> authService = new Mock<ICryptoService>();
        private readonly Mock<INeuronContracts> contracts = new Mock<INeuronContracts>();
        private readonly Mock<ISettingsService> settingsService = new Mock<ISettingsService>();
        private readonly Mock<INetworkService> networkService = new Mock<INetworkService>();
        private readonly Mock<ILogService> logService = new Mock<ILogService>();
        
        public ChooseAccountViewModelTests()
        {
            this.neuronService.Setup(x => x.Contracts).Returns(contracts.Object);
        }

        protected override ChooseAccountViewModel AViewModel()
        {
            return new ChooseAccountViewModel(new TagProfile(), dispatcher.Object, neuronService.Object, navigationService.Object, this.settingsService.Object, authService.Object, this.networkService.Object, this.logService.Object);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void When_CreateNewAccount_AndAccountNameIsEmpty_ThenButtonIsDisabled(string accountName)
        {
            Given(AViewModel)
                .And(vm => vm.CreateNewAccountName = accountName)
                .And(vm => vm.CreateNew = true)
                .ThenAssert(vm => vm.PerformActionCommand.IsDisabled());
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void When_ConnectToExistingAccount_AndAccountNameIsEmpty_ThenButtonIsDisabled(string accountName)
        {
            Given(AViewModel)
                .And(vm => vm.ConnectToExistingAccountName = accountName)
                .And(vm => vm.CreateNew = false)
                .ThenAssert(vm => vm.PerformActionCommand.IsDisabled());
        }
    }
}
