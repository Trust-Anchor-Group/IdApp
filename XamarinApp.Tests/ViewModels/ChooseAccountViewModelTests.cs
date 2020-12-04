using Moq;
using XamarinApp.Services;
using XamarinApp.Tests.Extensions;
using XamarinApp.ViewModels.Registration;
using NUnit.Framework;

namespace XamarinApp.Tests.ViewModels
{
    public class ChooseAccountViewModelTests : ViewModelTests<ChooseAccountViewModel>
    {
        private readonly Mock<INeuronService> tagService = new Mock<INeuronService>();
        private readonly Mock<INavigationService> navigationService = new Mock<INavigationService>();
        private readonly Mock<IAuthService> authService = new Mock<IAuthService>();
        private readonly Mock<IContractsService> contractsService = new Mock<IContractsService>();
        private readonly Mock<ISettingsService> settingsService = new Mock<ISettingsService>();
        private readonly Mock<INetworkService> networkService = new Mock<INetworkService>();
        private readonly Mock<ILogService> logService = new Mock<ILogService>();
        
        protected override ChooseAccountViewModel AViewModel()
        {
            return new ChooseAccountViewModel(new TagProfile(), tagService.Object, navigationService.Object, this.settingsService.Object, authService.Object, this.contractsService.Object, this.networkService.Object, this.logService.Object);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void When_CreateNewAccount_AndAccountNameIsEmpty_ThenButtonIsDisabled(string accountName)
        {
            Given(AViewModel)
                .And(vm => vm.AccountName = accountName)
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
                .And(vm => vm.AccountName = accountName)
                .And(vm => vm.CreateNew = false)
                .ThenAssert(vm => vm.PerformActionCommand.IsDisabled());
        }
    }
}
