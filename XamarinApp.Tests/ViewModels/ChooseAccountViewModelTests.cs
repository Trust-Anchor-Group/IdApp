using Moq;
using XamarinApp.Services;
using XamarinApp.Tests.Extensions;
using XamarinApp.ViewModels.Registration;
using NUnit.Framework;

namespace XamarinApp.Tests.ViewModels
{
    public class ChooseAccountViewModelTests : ViewModelTests<ChooseAccountViewModel>
    {
        private readonly Mock<ITagService> tagService = new Mock<ITagService>();
        private readonly Mock<IMessageService> messageService = new Mock<IMessageService>();
        private readonly Mock<IAuthService> authService = new Mock<IAuthService>();

        protected override ChooseAccountViewModel AViewModel()
        {
            return new ChooseAccountViewModel(RegistrationStep.Account, new TagProfile(), tagService.Object, messageService.Object, authService.Object);
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
